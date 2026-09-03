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
    // =========================================================
    // MY POSTS CONTROLLER
    // =========================================================
    //
    // This controller handles posts owned by the currently
    // logged-in Member.
    //
    // Responsibilities:
    // - Display the Member's Marketplace and Lost & Found posts
    // - Display owner-specific post details
    // - Soft delete posts
    // - Permanently delete uploaded post images
    // - Mark Marketplace listings as Sold / Adopted
    // - Mark Lost & Found reports as Resolved
    //
    // Only users with the Member role may access this controller.
    // =========================================================

    [Authorize(Roles = "Member")]
    public class MyPostsController : Controller
    {
        // =========================================================
        // DEPENDENCIES
        // =========================================================

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProfileService _profileService;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MyPostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProfileService profileService)
        {
            _context = context;
            _userManager = userManager;
            _profileService = profileService;
        }


        // =========================================================
        // MARKETPLACE DETAILS
        // =========================================================
        //
        // Displays a Marketplace listing owned by the currently
        // logged-in Member.
        //
        // Deleted listings cannot be opened anymore.
        // =========================================================

        public async Task<IActionResult> MarketplaceDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var listing = await _context.Listings
                .Include(l => l.Member)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l =>
                    l.ListingId == id &&
                    l.ListStatus != ListingStatus.Deleted);

            if (listing == null)
            {
                return NotFound();
            }

            // Only the owner may access this owner-specific page.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }

            return View(listing);
        }


        // =========================================================
        // LOST & FOUND DETAILS
        // =========================================================
        //
        // Displays a Lost & Found report owned by the currently
        // logged-in Member.
        //
        // Deleted reports cannot be opened anymore.
        // =========================================================

        public async Task<IActionResult> LostFoundDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var report = await _context.LostFounds
                .Include(r => r.User)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r =>
                    r.LostFoundId == id &&
                    r.RStatus != ReportStatus.Deleted);

            if (report == null)
            {
                return NotFound();
            }

            // Only the owner may access this owner-specific page.
            if (report.UserId != userId)
            {
                return Forbid();
            }

            return View(report);
        }


        // =========================================================
        // MY POSTS INDEX
        // =========================================================
        //
        // Displays all non-deleted Marketplace and Lost & Found
        // posts belonging to the currently logged-in Member.
        //
        // Supports:
        // - Approval Status filter
        // - Marketplace / Lost & Found type filter
        // =========================================================

        public async Task<IActionResult> Index(
            string? status,
            string? type)
        {
            // Get the currently logged-in Member's ID.
            var userId = _userManager.GetUserId(User);


            // =====================================================
            // BUILD VIEW MODEL
            // =====================================================

            var model = new MyPostsViewModel
            {
                StatusFilter = status,
                TypeFilter = type
            };


            // =====================================================
            // MARKETPLACE POSTS
            // =====================================================
            //
            // Deleted Marketplace listings are intentionally
            // excluded from My Posts.
            // =====================================================

            var listingsQuery = _context.Listings
                .Include(l => l.Images)
                .Where(l =>
                    l.MemberId == userId &&
                    l.ListStatus != ListingStatus.Deleted)
                .AsQueryable();


            // -----------------------------------------------------
            // Marketplace Approval Status Filter
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ListApprovalStatus>(
                    status,
                    out var listingStatus))
            {
                listingsQuery = listingsQuery
                    .Where(l => l.Status == listingStatus);
            }


            // -----------------------------------------------------
            // Post Type Filter
            // -----------------------------------------------------
            //
            // If Lost & Found was selected, remove Marketplace
            // posts from the result.
            // -----------------------------------------------------

            if (string.Equals(
                type,
                "LostFound",
                StringComparison.OrdinalIgnoreCase))
            {
                listingsQuery =
                    listingsQuery.Where(l => false);
            }


            var listings =
                await listingsQuery.ToListAsync();


            // =====================================================
            // LOST & FOUND POSTS
            // =====================================================
            //
            // Deleted Lost & Found reports are intentionally
            // excluded from My Posts.
            // =====================================================

            var reportsQuery = _context.LostFounds
                .Include(r => r.Images)
                .Where(r =>
                    r.UserId == userId &&
                    r.RStatus != ReportStatus.Deleted)
                .AsQueryable();


            // -----------------------------------------------------
            // Lost & Found Approval Status Filter
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ApprovalStatus>(
                    status,
                    out var reportStatus))
            {
                reportsQuery = reportsQuery
                    .Where(r => r.Status == reportStatus);
            }


            // -----------------------------------------------------
            // Post Type Filter
            // -----------------------------------------------------
            //
            // If Marketplace was selected, remove Lost & Found
            // reports from the result.
            // -----------------------------------------------------

            if (string.Equals(
                type,
                "Marketplace",
                StringComparison.OrdinalIgnoreCase))
            {
                reportsQuery =
                    reportsQuery.Where(r => false);
            }


            var reports =
                await reportsQuery.ToListAsync();


            // =====================================================
            // PROFILE INFORMATION
            // =====================================================

            var user =
                await _userManager.GetUserAsync(User);

            model.Profile =
                await _profileService
                    .BuildProfileViewModelAsync(user);


            // =====================================================
            // COMBINE MARKETPLACE + LOST & FOUND
            // =====================================================

            foreach (var listing in listings)
            {
                model.Posts.Add(
                    (listing, null)
                );
            }

            foreach (var report in reports)
            {
                model.Posts.Add(
                    (null, report)
                );
            }


            // =====================================================
            // SORT NEWEST FIRST
            // =====================================================

            model.Posts = model.Posts
                .OrderByDescending(post =>
                    post.Listing != null
                        ? post.Listing.DatePosted
                        : post.Report!.DateReported)
                .ToList();


            return View(model);
        }


        // =========================================================
        // DELETE MARKETPLACE LISTING
        // =========================================================
        //
        // IMPORTANT:
        // This is a SOFT DELETE.
        //
        // The Listing database record is preserved because other
        // records such as Conversations may still reference it.
        //
        // However:
        // - Physical uploaded image files ARE permanently deleted.
        // - ListingImage database records ARE permanently deleted.
        // - ListingStatus becomes Deleted.
        //
        // From the Member's point of view, the post is deleted.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMarketplace(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            // Retrieve the listing and its images.
            var listing = await _context.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(
                    l => l.ListingId == id
                );


            // Listing does not exist.
            if (listing == null)
            {
                return NotFound();
            }


            // Only the owner may delete the listing.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }


            // Prevent processing an already deleted listing.
            if (listing.ListStatus ==
                ListingStatus.Deleted)
            {
                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =====================================================
            // PERMANENTLY DELETE MARKETPLACE IMAGES
            // =====================================================

            if (listing.Images != null &&
                listing.Images.Any())
            {
                foreach (var image
                         in listing.Images.ToList())
                {
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        image.ImagePath.TrimStart('/')
                    );


                    // Delete the physical image from wwwroot.
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }


                    // Delete the image database record.
                    _context.ListingImages.Remove(image);
                }
            }


            // =====================================================
            // SOFT DELETE LISTING
            // =====================================================

            listing.ListStatus =
                ListingStatus.Deleted;


            await _context.SaveChangesAsync();


            // =====================================================
            // SYSTEM MODAL
            // =====================================================

            TempData["SuccessMessage"] =
                "Marketplace listing has been deleted.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // DELETE LOST & FOUND REPORT
        // =========================================================
        //
        // IMPORTANT:
        // This is also a SOFT DELETE.
        //
        // The LostFound database record is preserved so existing
        // conversations and other historical references do not
        // break.
        //
        // However:
        // - Physical report images ARE permanently deleted.
        // - LostFoundImage database records ARE permanently deleted.
        // - ReportStatus becomes Deleted.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLostFound(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            // Retrieve the report and its images.
            var report = await _context.LostFounds
                .Include(r => r.Images)
                .FirstOrDefaultAsync(
                    r => r.LostFoundId == id
                );


            // Report does not exist.
            if (report == null)
            {
                return NotFound();
            }


            // Only the owner may delete the report.
            if (report.UserId != userId)
            {
                return Forbid();
            }


            // Prevent processing an already deleted report.
            if (report.RStatus ==
                ReportStatus.Deleted)
            {
                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =====================================================
            // PERMANENTLY DELETE LOST & FOUND IMAGES
            // =====================================================

            if (report.Images != null &&
                report.Images.Any())
            {
                foreach (var image
                         in report.Images.ToList())
                {
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        image.ImagePath.TrimStart('/')
                    );


                    // Delete the physical image from wwwroot.
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }


                    // Delete the image database record.
                    _context.LostFoundImages.Remove(image);
                }
            }


            // =====================================================
            // SOFT DELETE REPORT
            // =====================================================

            report.RStatus =
                ReportStatus.Deleted;


            await _context.SaveChangesAsync();


            // =====================================================
            // SYSTEM MODAL
            // =====================================================

            TempData["SuccessMessage"] =
                "Lost & Found report has been deleted.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // MARK MARKETPLACE LISTING AS SOLD
        // =========================================================
        //
        // Only:
        // - Owner
        // - Approved listing
        // - Currently available listing
        //
        // may perform this action.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var listing = await _context.Listings
                .FirstOrDefaultAsync(
                    l => l.ListingId == id
                );


            if (listing == null)
            {
                return NotFound();
            }


            // Only owner may perform this action.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }


            // Listing must already be approved.
            if (listing.Status !=
                ListApprovalStatus.Approved)
            {
                return Forbid();
            }


            // Listing must still be available.
            if (listing.ListStatus !=
                ListingStatus.Pending)
            {
                return Forbid();
            }


            listing.ListStatus =
                ListingStatus.Sold;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Marketplace listing has been marked as sold.";


            return RedirectToAction(
                nameof(MarketplaceDetails),
                new
                {
                    id = listing.ListingId
                }
            );
        }


        // =========================================================
        // MARK MARKETPLACE LISTING AS ADOPTED
        // =========================================================
        //
        // Only:
        // - Owner
        // - Approved listing
        // - Currently available listing
        //
        // may perform this action.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsAdopted(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var listing = await _context.Listings
                .FirstOrDefaultAsync(
                    l => l.ListingId == id
                );


            if (listing == null)
            {
                return NotFound();
            }


            // Only owner may perform this action.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }


            // Listing must already be approved.
            if (listing.Status !=
                ListApprovalStatus.Approved)
            {
                return Forbid();
            }


            // Listing must still be available.
            if (listing.ListStatus !=
                ListingStatus.Pending)
            {
                return Forbid();
            }


            listing.ListStatus =
                ListingStatus.Adopted;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Marketplace listing has been marked as adopted.";


            return RedirectToAction(
                nameof(MarketplaceDetails),
                new
                {
                    id = listing.ListingId
                }
            );
        }


        // =========================================================
        // MARK LOST & FOUND REPORT AS RESOLVED
        // =========================================================
        //
        // Lost report:
        //     "Mark as Found"
        //
        // Found report:
        //     "Mark as Resolved"
        //
        // Only approved and active reports may be resolved.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReportResolved(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var report = await _context.LostFounds
                .FirstOrDefaultAsync(
                    r => r.LostFoundId == id
                );


            if (report == null)
            {
                return NotFound();
            }


            // Only owner may perform this action.
            if (report.UserId != userId)
            {
                return Forbid();
            }


            // Report must already be approved.
            if (report.Status !=
                ApprovalStatus.Approved)
            {
                return Forbid();
            }


            // Report must still be active.
            if (report.RStatus !=
                ReportStatus.Active)
            {
                return Forbid();
            }


            report.RStatus =
                ReportStatus.Resolved;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                report.Type == LostFoundType.Lost
                    ? "Lost pet report has been marked as found."
                    : "Found pet report has been marked as resolved.";


            return RedirectToAction(
                nameof(LostFoundDetails),
                new
                {
                    id = report.LostFoundId
                }
            );
        }
    }
}