using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    // Restricts the entire controller to authenticated Members.
    // Administrators and unauthenticated users cannot directly submit reports.
    [Authorize(Roles = "Member")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Dependency Injection provides the database context and Identity UserManager.
        // ApplicationDbContext handles UserReport, Listing, and LostFound database operations.
        // UserManager retrieves the currently authenticated member's Identity information.
        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: Reports/Create
        // Receives a report submitted by a member.
        // The CreateReportViewModel contains the reported content type,
        // content ID, selected reason, optional custom reason, and description.
        // Model validation is performed before the controller processes the report.
        // [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReportViewModel model)
        {
            // Get the ID of the currently authenticated member through ASP.NET Identity.
            // If no ID can be retrieved, the request is rejected because reports require
            // an authenticated Member account.
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // The reported content must exist before a UserReport can be created.
            // These variables also allow us to perform ownership and duplicate checks
            // against the correct type of PETHUB post.
            Listing? listing = null;
            LostFound? lostFound = null;

            // Load the Marketplace listing when the submitted content type is Listing.
            if (model.ContentType == ReportedContentType.Listing)
            {
                listing = await _context.Listings
                    .FirstOrDefaultAsync(l => l.ListingId == model.ContentId);

                // A report cannot be created for a Marketplace listing that no longer exists.
                if (listing == null)
                {
                    return NotFound();
                }

                // Members are not allowed to report their own Marketplace listings.
                // This check is performed on the server so it cannot be bypassed by
                // manually changing the submitted content ID.
                if (listing.MemberId == userId)
                {
                    return Forbid();
                }
            }
            // Load the Lost & Found post when the submitted content type is LostFound.
            else if (model.ContentType == ReportedContentType.LostFound)
            {
                lostFound = await _context.LostFounds
                    .FirstOrDefaultAsync(l => l.LostFoundId == model.ContentId);

                // A report cannot be created for a Lost & Found post that no longer exists.
                if (lostFound == null)
                {
                    return NotFound();
                }

                // Only Lost & Found posts belonging to registered PETHUB members
                // can participate in the member reporting system. Unregistered client
                // reports have UserId == null, so they are explicitly rejected here.
                if (string.IsNullOrEmpty(lostFound.UserId))
                {
                    return Forbid();
                }

                // Prevent a member from reporting their own Lost & Found post.
                // The authenticated member's ID is compared with the registered owner's ID.
                if (lostFound.UserId == userId)
                {
                    return Forbid();
                }
            }
            else
            {
                // Reject invalid enum values instead of allowing an unknown content
                // type to create an incomplete UserReport record.
                return BadRequest();
            }

            // Prevent duplicate reports while an existing report is still active.
            // Dismissed reports are intentionally excluded so the same member can
            // report the same post again after an administrator dismisses the first report.
            var existingReport = await _context.UserReports
                .AnyAsync(r =>
                    r.ReporterId == userId &&
                    r.ContentType == model.ContentType &&
                    (
                        (model.ContentType == ReportedContentType.Listing &&
                         r.ListingId == model.ContentId)
                        ||
                        (model.ContentType == ReportedContentType.LostFound &&
                         r.LostFoundId == model.ContentId)
                    ) &&
                    r.Status != UserReportStatus.Dismissed);

            if (existingReport)
            {
                // Conflict is appropriate here because the member already has
                // a non-dismissed report for the same content.
                return Conflict("You have already reported this post.");
            }

            // If the member selected a predefined reason, the custom OtherReason
            // value is not needed and is discarded.
            if (model.Reason != UserReportReason.Other)
            {
                model.OtherReason = null;
            }

            // Create the new report using the authenticated member's ID.
            // The status defaults to Pending through the UserReport model.
            var report = new UserReport
            {
                ReporterId = userId,
                ContentType = model.ContentType,
                Reason = model.Reason,
                OtherReason = model.OtherReason,
                Description = model.Description,
                Status = UserReportStatus.Pending,
                DateCreated = DateTime.UtcNow
            };

            // Store the foreign key for the correct reported content.
            // Only one of ListingId or LostFoundId is populated for each report.
            if (model.ContentType == ReportedContentType.Listing)
            {
                report.ListingId = model.ContentId;
            }
            else
            {
                report.LostFoundId = model.ContentId;
            }

            // Add the completed UserReport entity to Entity Framework's change tracker.
            _context.UserReports.Add(report);

            // Save the new report to SQL Server.
            await _context.SaveChangesAsync();

            // Phase 2 UI will later determine the appropriate return destination.
            // For now, redirect the member to the Home page after a successful submission.
            return RedirectToAction("Index", "Home");
        }
    }
}