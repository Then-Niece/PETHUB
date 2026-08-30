using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    // Restricts Removed Posts to authenticated Members.
    // This prevents users from accessing moderation information belonging
    // to accounts other than their own.
    [Authorize(Roles = "Member")]
    public class RemovedPostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RemovedPostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // INDEX
        // =========================================================

        // Displays all Marketplace and Lost & Found posts that were
        // removed from the currently authenticated Member.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Gets the Identity ID of the currently logged-in Member.
            // This is the value that must match Listing.MemberId or LostFound.UserId.
            var userId = _userManager.GetUserId(User);

            // If no Identity ID is available, the current request is not
            // associated with an authenticated Member.
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var allRemovedListings = await _context.Listings
                .Where(l => l.Status == ListApprovalStatus.Removed)
                .Select(l => new
                {
                    l.ListingId,
                    l.MemberId,
                    l.Title
                })
                .ToListAsync();


            // =========================================================
            // MARKETPLACE - CURRENT MEMBER'S REMOVED RECORDS
            // =========================================================

            // Retrieves only Removed Marketplace listings owned by
            // the currently authenticated Member.
            var removedListings = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Member)
                .Where(l =>
                    l.MemberId == userId &&
                    l.Status == ListApprovalStatus.Removed)
                .ToListAsync();


            // =========================================================
            // MARKETPLACE VIEW DATA
            // =========================================================

            // Stores each removed Marketplace listing together with
            // its Admin removal reason.
            var removedListingData =
                new List<(Listing Listing, string? AdminActionReason)>();


            // Retrieves the Admin removal reason for every matching listing.
            foreach (var listing in removedListings)
            {
                // Finds the latest resolved report associated with this
                // exact Marketplace listing.
                var resolvedReport = await _context.UserReports
                    .Where(r =>
                        r.ContentType == ReportedContentType.Listing &&
                        r.ListingId == listing.ListingId &&
                        r.Status == UserReportStatus.Resolved)
                    .OrderByDescending(r => r.UserReportId)
                    .FirstOrDefaultAsync();

                // Adds the existing Listing and its Admin reason to the
                // collection used by RemovedPostsViewModel.
                removedListingData.Add(
                    (listing, resolvedReport?.AdminActionReason)
                );
            }


            // =========================================================
            // LOST & FOUND - ALL REMOVED RECORDS
            // =========================================================

            // Temporarily retrieves every Removed Lost & Found post
            // without applying the current user's ownership filter.
            var allRemovedLostFound = await _context.LostFounds
                .Where(l => l.Status == ApprovalStatus.Removed)
                .Select(l => new
                {
                    l.LostFoundId,
                    l.UserId,
                    l.Title
                })
                .ToListAsync();


            // =========================================================
            // LOST & FOUND - CURRENT MEMBER'S REMOVED RECORDS
            // =========================================================

            // Retrieves only Removed Lost & Found posts owned by
            // the currently authenticated Member.
            var removedLostFound = await _context.LostFounds
                .Include(l => l.Images)
                .Include(l => l.User)
                .Where(l =>
                    l.UserId == userId &&
                    l.Status == ApprovalStatus.Removed)
                .ToListAsync();


            // =========================================================
            // LOST & FOUND VIEW DATA
            // =========================================================

            // Stores each removed Lost & Found post together with
            // the Admin's removal reason.
            var removedLostFoundData =
                new List<(LostFound Report, string? AdminActionReason)>();


            // Retrieves the Admin removal reason for every matching
            // Lost & Found post.
            foreach (var lostFound in removedLostFound)
            {
                // Finds the latest resolved report associated with this
                // exact Lost & Found post.
                var resolvedReport = await _context.UserReports
                    .Where(r =>
                        r.ContentType == ReportedContentType.LostFound &&
                        r.LostFoundId == lostFound.LostFoundId &&
                        r.Status == UserReportStatus.Resolved)
                    .OrderByDescending(r => r.UserReportId)
                    .FirstOrDefaultAsync();

                // Adds the existing Lost & Found post and its Admin
                // reason to the ViewModel collection.
                removedLostFoundData.Add(
                    (lostFound, resolvedReport?.AdminActionReason)
                );
            }


            // =========================================================
            // BUILD VIEWMODEL
            // =========================================================

            // Creates the strongly typed model expected by
            // Views/RemovedPosts/Index.cshtml.
            //
            // The collections are initialized even when no records exist,
            // so the Razor view can safely use .Any() and foreach.
            var model = new RemovedPostsViewModel
            {
                RemovedListings = removedListingData,
                RemovedLostFound = removedLostFoundData
            };


            // Sends the populated ViewModel to the Removed Posts Index view.
            return View(model);
        }


        // =========================================================
        // DETAILS
        // =========================================================

        // Displays the complete information for one removed post.
        // The post must belong to the currently authenticated Member.
        [HttpGet]
        public async Task<IActionResult> Details(string type, int id)
        {
            // Gets the currently authenticated Member's Identity ID.
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Reject invalid IDs before querying the database.
            if (id <= 0)
            {
                return NotFound();
            }


            // =========================================================
            // MARKETPLACE LISTING
            // =========================================================

            if (string.Equals(
                    type,
                    "listing",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Retrieves the existing removed Marketplace listing.
                // Ownership is verified against the current Identity ID.
                var listing = await _context.Listings
                    .Include(l => l.Images)
                    .Include(l => l.Member)
                    .FirstOrDefaultAsync(l =>
                        l.ListingId == id &&
                        l.MemberId == userId &&
                        l.Status == ListApprovalStatus.Removed);

                if (listing == null)
                {
                    return NotFound();
                }


                // Finds the latest resolved report responsible for
                // removing this Marketplace listing.
                var resolvedReport = await _context.UserReports
                    .Where(r =>
                        r.ContentType == ReportedContentType.Listing &&
                        r.ListingId == listing.ListingId &&
                        r.Status == UserReportStatus.Resolved)
                    .OrderByDescending(r => r.UserReportId)
                    .FirstOrDefaultAsync();

                // Gets the latest appeal submitted for this exact Marketplace listing.
                // If the Member has never appealed this post, the result is null.
                var appeal = await _context.Appeals
                    .Where(a => a.ListingId == listing.ListingId)
                    .OrderByDescending(a => a.AppealId)
                    .FirstOrDefaultAsync();

                // Creates the Details ViewModel using the existing listing.
                var model = new RemovedPostsViewModel
                {
                    PostType = "listing",
                    Listing = listing,
                    LostFound = null,
                    AdminActionReason = resolvedReport?.AdminActionReason,
                    Appeal = appeal
                };


                return View(model);
            }


            // =========================================================
            // LOST & FOUND
            // =========================================================

            if (string.Equals(
                    type,
                    "lostfound",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Retrieves the existing removed Lost & Found post.
                // Ownership is verified against the current Identity ID.
                var lostFound = await _context.LostFounds
                    .Include(l => l.Images)
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l =>
                        l.LostFoundId == id &&
                        l.UserId == userId &&
                        l.Status == ApprovalStatus.Removed);

                if (lostFound == null)
                {
                    return NotFound();
                }


                // Finds the latest resolved report responsible for
                // removing this Lost & Found post.
                var resolvedReport = await _context.UserReports
                    .Where(r =>
                        r.ContentType == ReportedContentType.LostFound &&
                        r.LostFoundId == lostFound.LostFoundId &&
                        r.Status == UserReportStatus.Resolved)
                    .OrderByDescending(r => r.UserReportId)
                    .FirstOrDefaultAsync();

                // Gets the latest appeal submitted for this exact Lost & Found post.
                // If the Member has never appealed this post, the result is null.
                var appeal = await _context.Appeals
                    .Where(a => a.LostFoundId == lostFound.LostFoundId)
                    .OrderByDescending(a => a.AppealId)
                    .FirstOrDefaultAsync();

                // Creates the Details ViewModel using the existing
                // Lost & Found post.
                var model = new RemovedPostsViewModel
                {
                    PostType = "lostfound",
                    Listing = null,
                    LostFound = lostFound,
                    AdminActionReason = resolvedReport?.AdminActionReason,
                    Appeal = appeal
                };


                return View(model);
            }


            // Reject unsupported post types.
            return NotFound();
        }
    }
}