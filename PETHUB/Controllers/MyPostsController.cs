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
    // Handles Marketplace and Lost & Found posts owned by
    // the currently authenticated Member.
    //
    // Responsibilities:
    // - Display Member-owned posts
    // - Filter and paginate posts
    // - Display owner-specific details
    // - Soft delete posts
    // - Permanently remove uploaded images during deletion
    // - Mark listings as Sold / Adopted
    // - Mark Lost & Found reports as Resolved
    //
    // Only Members may access this controller.
    // =========================================================

    [Authorize(Roles = "Member")]
    public class MyPostsController : Controller
    {
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
        // Soft-deleted listings cannot be opened.
        // =========================================================

        public async Task<IActionResult> MarketplaceDetails(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var listing =
                await _context.Listings
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
        // Soft-deleted reports cannot be opened.
        // =========================================================

        public async Task<IActionResult> LostFoundDetails(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var report =
                await _context.LostFounds
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
        // posts belonging to the currently authenticated Member.
        //
        // Supports:
        // - Approval Status filter
        // - Marketplace / Lost & Found filter
        // - Combined newest-first sorting
        // - Pagination
        // =========================================================

        public async Task<IActionResult> Index(
            string? status,
            string? type,
            int page = 1)
        {
            const int pageSize = 12;


            // Prevent invalid page numbers.
            if (page < 1)
            {
                page = 1;
            }


            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }


            // =====================================================
            // BUILD VIEW MODEL
            // =====================================================

            var model =
                new MyPostsViewModel
                {
                    StatusFilter = status,
                    TypeFilter = type
                };


            // =====================================================
            // MARKETPLACE POSTS
            // =====================================================
            //
            // Deleted Marketplace listings remain in the database
            // for historical relationships, but are hidden here.
            // =====================================================

            var listingsQuery =
                _context.Listings
                    .Include(l => l.Images)
                    .Where(l =>
                        l.MemberId == userId &&
                        l.ListStatus != ListingStatus.Deleted)
                    .AsQueryable();


            // -----------------------------------------------------
            // MARKETPLACE APPROVAL STATUS FILTER
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ListApprovalStatus>(
                    status,
                    out var listingStatus))
            {
                listingsQuery =
                    listingsQuery.Where(
                        l => l.Status == listingStatus);
            }


            // -----------------------------------------------------
            // POST TYPE FILTER
            // -----------------------------------------------------
            //
            // If Lost & Found was selected, exclude Marketplace.
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
            // Deleted reports are preserved for historical
            // relationships but are hidden from My Posts.
            // =====================================================

            var reportsQuery =
                _context.LostFounds
                    .Include(r => r.Images)
                    .Where(r =>
                        r.UserId == userId &&
                        r.RStatus != ReportStatus.Deleted)
                    .AsQueryable();


            // -----------------------------------------------------
            // LOST & FOUND APPROVAL STATUS FILTER
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ApprovalStatus>(
                    status,
                    out var reportStatus))
            {
                reportsQuery =
                    reportsQuery.Where(
                        r => r.Status == reportStatus);
            }


            // -----------------------------------------------------
            // POST TYPE FILTER
            // -----------------------------------------------------
            //
            // If Marketplace was selected, exclude Lost & Found.
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


            if (user == null)
            {
                return Unauthorized();
            }


            model.Profile =
                await _profileService
                    .BuildProfileViewModelAsync(user);


            // =====================================================
            // COMBINE MARKETPLACE + LOST & FOUND
            // =====================================================

            foreach (var listing in listings)
            {
                model.Posts.Add(
                    (listing, null));
            }


            foreach (var report in reports)
            {
                model.Posts.Add(
                    (null, report));
            }


            // =====================================================
            // SORT NEWEST FIRST
            // =====================================================

            model.Posts =
                model.Posts
                    .OrderByDescending(post =>
                        post.Listing != null
                            ? post.Listing.DatePosted
                            : post.Report!.DateReported)
                    .ToList();


            // =====================================================
            // PAGINATION
            // =====================================================
            //
            // Pagination is applied AFTER Marketplace and Lost &
            // Found posts have been combined and sorted so that
            // both post types share one correct timeline.
            // =====================================================

            var totalItems =
                model.Posts.Count;


            var totalPages =
                (int)Math.Ceiling(
                    totalItems / (double)pageSize);


            if (totalPages > 0 &&
                page > totalPages)
            {
                page = totalPages;
            }


            model.Posts =
                model.Posts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();


            model.CurrentPage = page;
            model.PageSize = pageSize;
            model.TotalItems = totalItems;


            return View(model);
        }


        // =========================================================
        // DELETE MARKETPLACE LISTING
        // =========================================================
        //
        // IMPORTANT:
        // This is a SOFT DELETE.
        //
        // The Listing database record is preserved because
        // Conversations and other historical records may still
        // reference it.
        //
        // However:
        // - Physical uploaded images ARE permanently deleted.
        // - ListingImage records ARE permanently deleted.
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


            var listing =
                await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


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
                TempData["InfoMessage"] =
                    "This Marketplace listing has already been deleted.";

                return RedirectToAction(
                    nameof(Index));
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
                    var filePath =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            image.ImagePath.TrimStart('/'));


                    // Delete the physical uploaded image.
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }


                    // Delete only the related image database record.
                    _context.ListingImages.Remove(image);
                }
            }


            // =====================================================
            // SOFT DELETE LISTING
            // =====================================================

            listing.ListStatus =
                ListingStatus.Deleted;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Marketplace listing has been deleted.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // DELETE LOST & FOUND REPORT
        // =========================================================
        //
        // IMPORTANT:
        // This is also a SOFT DELETE.
        //
        // The LostFound database record is preserved so existing
        // Conversations and historical references remain valid.
        //
        // However:
        // - Physical uploaded images ARE permanently deleted.
        // - LostFoundImage records ARE permanently deleted.
        // - ReportStatus becomes Deleted.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLostFound(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var report =
                await _context.LostFounds
                    .Include(r => r.Images)
                    .FirstOrDefaultAsync(
                        r => r.LostFoundId == id);


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
                TempData["InfoMessage"] =
                    "This Lost & Found report has already been deleted.";

                return RedirectToAction(
                    nameof(Index));
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
                    var filePath =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            image.ImagePath.TrimStart('/'));


                    // Delete the physical uploaded image.
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }


                    // Delete only the related image database record.
                    _context.LostFoundImages.Remove(image);
                }
            }


            // =====================================================
            // SOFT DELETE REPORT
            // =====================================================

            report.RStatus =
                ReportStatus.Deleted;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Lost & Found report has been deleted.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // MARK MARKETPLACE LISTING AS SOLD
        // =========================================================
        //
        // Only the owner of an Approved and currently available
        // For Sale listing may perform this action.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var listing =
                await _context.Listings
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


            if (listing == null)
            {
                return NotFound();
            }


            if (listing.MemberId != userId)
            {
                return Forbid();
            }


            if (listing.Status !=
                ListApprovalStatus.Approved)
            {
                return Forbid();
            }


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
                });
        }


        // =========================================================
        // MARK MARKETPLACE LISTING AS ADOPTED
        // =========================================================
        //
        // Only the owner of an Approved and currently available
        // adoption listing may perform this action.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsAdopted(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var listing =
                await _context.Listings
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


            if (listing == null)
            {
                return NotFound();
            }


            if (listing.MemberId != userId)
            {
                return Forbid();
            }


            if (listing.Status !=
                ListApprovalStatus.Approved)
            {
                return Forbid();
            }


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
                });
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
        // Only the owner of an Approved and Active report
        // may perform this action.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReportResolved(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            var report =
                await _context.LostFounds
                    .FirstOrDefaultAsync(
                        r => r.LostFoundId == id);


            if (report == null)
            {
                return NotFound();
            }


            if (report.UserId != userId)
            {
                return Forbid();
            }


            if (report.Status !=
                ApprovalStatus.Approved)
            {
                return Forbid();
            }


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
                });
        }
    }
}