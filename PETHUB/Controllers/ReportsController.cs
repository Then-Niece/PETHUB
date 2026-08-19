using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        // Dependency Injection provides the database context and Identity UserManager.
        // ApplicationDbContext handles UserReport, Listing, and LostFound database operations.
        // UserManager retrieves the currently authenticated member's Identity information.
        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            // Provides database access for reports and reported content.
            _context = context;

            // Provides the authenticated user's ID and access to Admin accounts.
            _userManager = userManager;

            // Provides the existing PETHUB notification functionality.
            _notificationService = notificationService;
        }        // POST: Reports/Create
        // Allows only Members to submit a new report. Admins can access the controller
        // but cannot use the member report-submission action.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
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

            // Add the completed UserReport to EF Core's change tracker.
            _context.UserReports.Add(report);

            // Save the report first so the new Pending report exists in the database
            // before the Admin notification count is calculated.
            await _context.SaveChangesAsync();

            // Retrieve all Admin accounts using the same existing PETHUB pattern
            // already used by ListingsController and LostFoundsController.
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            // Update each Admin's single aggregate report notification.
            // If this is the first Pending report, the notification is created.
            // If the notification already exists, its message is updated instead of
            // creating another notification.
            await _notificationService.UpdateAdminReportNotificationAsync(admins);

            // Return the Member to the existing Home page after successful submission.
            return RedirectToAction("Index", "Home");
        }

        // GET: Reports
        // Displays the dedicated Administrator Reports page.
        // Only Admin accounts can review submitted UserReports.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(
            string? reportStatus,
            string? reportType)
        {
            // Load the Reporter and the owner of the reported content.
            // Listing.Member is the Marketplace post owner, while LostFound.User
            // is the registered Lost & Found post owner.
            var reports = _context.UserReports
                .Include(r => r.Reporter)
                .Include(r => r.Listing)
                    .ThenInclude(l => l!.Member)
                .Include(r => r.LostFound)
                    .ThenInclude(l => l!.User)
                .AsQueryable();

            // Filter by the report's moderation status when the Admin
            // selects Pending, Dismissed, or Resolved.
            if (!string.IsNullOrWhiteSpace(reportStatus) &&
                Enum.TryParse<UserReportStatus>(
                    reportStatus,
                    true,
                    out var selectedStatus))
            {
                // EF Core converts this into a SQL WHERE condition.
                reports = reports.Where(r => r.Status == selectedStatus);
            }

            // Filter by the type of content that was reported.
            if (!string.IsNullOrWhiteSpace(reportType) &&
                Enum.TryParse<ReportedContentType>(
                    reportType,
                    true,
                    out var selectedType))
            {
                // Only reports targeting the selected content type are returned.
                reports = reports.Where(r => r.ContentType == selectedType);
            }

            // Display newest reports first so the newest submissions
            // appear at the top of the Administrator's review queue.
            reports = reports.OrderByDescending(r => r.DateCreated);

            // Create the reusable local filter bar.
            // The selected values come directly from the Index action parameters.
            // This avoids using Razor's Context object inside the controller.
            var filters = PETHUB.Helpers.FilterBarHelper.Create(
                PETHUB.Helpers.FilterBarHelper.ReportStatus(
                    reportStatus
                ),
                PETHUB.Helpers.FilterBarHelper.ReportPostType(
                    reportType
                )
            );

            // Pass the filtered reports and filter configuration to the view.
            ViewData["ReportFilters"] = filters;

            // Render the dedicated Admin Reports page.
            return View(
                "~/Views/AdminReports/Index.cshtml",
                await reports.ToListAsync()
            );
        }

        // GET: Reports/Details/5
        // Displays the complete report and the content being reported.
        // Only Admin accounts can access the report-review page.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            // A report ID is required to locate the specific UserReport.
            // If no ID was supplied, return a standard 404 response.
            if (id == null)
            {
                return NotFound();
            }

            // Load the Reporter, reported content owner, and images.
            // Listing.Member identifies the Marketplace listing owner.
            // LostFound.User identifies the registered Lost & Found post owner.
            var report = await _context.UserReports
                .Include(r => r.Reporter)
                .Include(r => r.Listing)
                    .ThenInclude(l => l!.Member)
                .Include(r => r.Listing)
                    .ThenInclude(l => l!.Images)
                .Include(r => r.LostFound)
                    .ThenInclude(l => l!.User)
                .Include(r => r.LostFound)
                    .ThenInclude(l => l!.Images)
                .FirstOrDefaultAsync(r => r.UserReportId == id);

            // If the report no longer exists, there is nothing for the Admin to review.
            if (report == null)
            {
                return NotFound();
            }

            // Render the dedicated Admin Reports Details view.
            // The view is intentionally stored under Views/AdminReports.
            return View(
                "~/Views/AdminReports/Details.cshtml",
                report
            );
        }

        // POST: Reports/Dismiss
        // Marks a pending UserReport as Dismissed without deleting the reported post.
        // Only Admin accounts are allowed to perform this moderation action.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dismiss(int id)
        {
            // Find the specific report selected by the Administrator.
            // FirstOrDefaultAsync returns the matching UserReport or null if it
            // no longer exists in the database.
            var report = await _context.UserReports
                .FirstOrDefaultAsync(r => r.UserReportId == id);

            // Stop if the requested report does not exist.
            if (report == null)
            {
                return NotFound();
            }

            // A report should only be dismissed while it is still waiting for review.
            // This prevents an already resolved or dismissed report from being changed
            // accidentally by submitting an old form again.
            if (report.Status != UserReportStatus.Pending)
            {
                return BadRequest("Only pending reports can be dismissed.");
            }

            // Mark the report as dismissed.
            // The reported post remains available because no violation was confirmed.
            report.Status = UserReportStatus.Dismissed;

            // Save the report status before creating the outcome notification
            // and recalculating the Admin's Pending report count.
            await _context.SaveChangesAsync();

            // Notify the Member who originally submitted the report.
            // Only the Reporter receives an outcome notification when a report is dismissed.
            await _notificationService.CreateNotificationAsync(
                report.ReporterId,
                NotificationType.UserReportRejected,
                "Report Rejected",
                "Your report was reviewed by an administrator and no violation was confirmed."
            );

            // Retrieve all Admin accounts using the existing PETHUB Identity pattern.
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            // Update the single aggregate Admin report notification.
            // The count decreases after this report leaves the Pending state.
            await _notificationService.UpdateAdminReportNotificationAsync(admins);

            // Return to the Admin Reports page after the moderation action succeeds.
            return RedirectToAction(nameof(Index));
        }

        // POST: Reports/ConfirmViolation
        // Confirms that the reported content violates PETHUB rules.
        // The reported post and its associated images are deleted, but the
        // UserReport itself is preserved as a moderation record.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmViolation(int id)
        {
            // Load the report together with the reported Marketplace listing,
            // Lost & Found post, and their image collections.
            // Only one of Listing or LostFound should be populated for a valid report.
            var report = await _context.UserReports
                .Include(r => r.Listing)
                    .ThenInclude(l => l!.Images)
                .Include(r => r.LostFound)
                    .ThenInclude(l => l!.Images)
                .FirstOrDefaultAsync(r => r.UserReportId == id);

            // Stop if the requested report does not exist.
            if (report == null)
            {
                return NotFound();
            }

            // Only Pending reports can be confirmed.
            // This prevents an already dismissed or resolved report from being
            // processed again through an old or duplicated request.
            if (report.Status != UserReportStatus.Pending)
            {
                return BadRequest("Only pending reports can be confirmed.");
            }

            string? reportedUserId = null;

            // Handle a reported Marketplace listing.
            if (report.ContentType == ReportedContentType.Listing)
            {
                // The listing may already have been deleted outside the report system.
                // In that case, the report can still be resolved without attempting
                // to delete a nonexistent listing.
                if (report.Listing != null)
                {
                    // Delete each physical image file associated with the listing.
                    // File.Delete removes the file from the server's file system.
                    if (report.Listing.Images != null)
                    {
                        foreach (var image in report.Listing.Images)
                        {
                            // Convert the stored web path into the application's
                            // physical wwwroot path before deleting the file.
                            var imagePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                image.ImagePath.TrimStart('/', '\\')
                            );

                            // Only attempt deletion when the physical file exists.
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }

                        // Remove the ListingImages records from the database.
                        _context.ListingImages.RemoveRange(report.Listing.Images);
                    }

                    // Capture the Marketplace listing owner's ID before deleting the listing.
                    // This ID is needed to notify the user that their post was removed.
                    reportedUserId = report.Listing.MemberId;

                    // Remove the reported Marketplace listing itself.
                    _context.Listings.Remove(report.Listing);
                }
            }
            // Handle a reported Lost & Found post.
            else if (report.ContentType == ReportedContentType.LostFound)
            {
                // The Lost & Found post may already have been deleted elsewhere.
                if (report.LostFound != null)
                {
                    // Delete each physical Lost & Found image file first.
                    if (report.LostFound.Images != null)
                    {
                        foreach (var image in report.LostFound.Images)
                        {
                            // Convert the stored web path into the application's
                            // physical wwwroot path before deleting the file.
                            var imagePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                image.ImagePath.TrimStart('/', '\\')
                            );

                            // File.Exists prevents an exception when an image file
                            // is already missing from the server.
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }

                        // Remove the LostFoundImages records from the database.
                        _context.LostFoundImages.RemoveRange(report.LostFound.Images);
                    }

                    // Capture the Lost & Found post owner's ID before deleting the post.
                    // This ID is needed to notify the user that their post was removed.
                    reportedUserId = report.LostFound.UserId;

                    // Remove the reported Lost & Found post itself.
                    _context.LostFounds.Remove(report.LostFound);
                }
            }
            else
            {
                // Reject an invalid ContentType instead of resolving an incomplete
                // or unsupported report.
                return BadRequest("Invalid report content type.");
            }


            // Mark the report as resolved because the reported content was confirmed
            // to violate PETHUB rules and has been removed.
            report.Status = UserReportStatus.Resolved;

            // Save the post deletion and the resolved report status first.
            // The UserReport itself remains in the database as moderation history.
            await _context.SaveChangesAsync();

            // Notify the Member who submitted the report.
            // The report was accepted because the Administrator confirmed a violation
            // and removed the reported content.
            await _notificationService.CreateNotificationAsync(
                report.ReporterId,
                NotificationType.UserReportAccepted,
                "Report Accepted",
                "Your report was reviewed by an administrator and the reported post was removed."
            );

            // Notify the owner of the removed post.
            // This notification is only sent when a violation is confirmed.
            if (!string.IsNullOrEmpty(reportedUserId))
            {
                // Create a notification for the user whose post was removed.
                // The notification explains that the post was removed after administrative review.
                await _notificationService.CreateNotificationAsync(
                    reportedUserId,
                    NotificationType.ReportedPostRemoved,
                    "Your Post Was Removed",
                    "Your post was reviewed by an administrator and was removed because it was found to violate PETHUB rules."
                );
            }

            // Retrieve all Admin accounts using the existing PETHUB notification pattern.
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            // Recalculate the single aggregate Admin report notification.
            // The count decreases because this report is no longer Pending.
            // If this was the final Pending report, the notification is deleted.
            await _notificationService.UpdateAdminReportNotificationAsync(admins);

            // Return to the Admin Reports page after successful moderation.
            return RedirectToAction(nameof(Index));
        }
    }
}