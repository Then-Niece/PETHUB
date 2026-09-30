using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    [Authorize(Roles = "Member")]
    public class SavedItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SavedItemsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // SAVED ITEMS PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var memberId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(memberId))
            {
                return Unauthorized();
            }


            // =====================================================
            // MARKETPLACE
            // =====================================================

            var savedListings =
                await _context.SavedListings
                    .Where(s =>
                        s.MemberId == memberId
                        &&
                        s.Listing != null
                        &&
                        s.Listing.Status ==
                            ListApprovalStatus.Approved
                        &&
                        s.Listing.ListStatus ==
                            ListingStatus.Pending
                        &&
                        s.Listing.Member != null
                        &&
                        s.Listing.Member.Status ==
                            UserStatus.Active)
                    .Include(s => s.Listing)
                        .ThenInclude(l => l!.Images)
                    .Include(s => s.Listing)
                        .ThenInclude(l => l!.Member)
                    .OrderByDescending(s => s.DateSaved)
                    .ToListAsync();


            // =====================================================
            // LOST & FOUND
            // =====================================================

            var savedLostFounds =
                await _context.SavedLostFounds
                    .Where(s =>
                        s.MemberId == memberId
                        &&
                        s.LostFound != null
                        &&
                        s.LostFound.Status ==
                            ApprovalStatus.Approved
                        &&
                        s.LostFound.RStatus ==
                            ReportStatus.Active
                        &&
                        (
                            s.LostFound.UserId == null
                            ||
                            (
                                s.LostFound.User != null
                                &&
                                s.LostFound.User.Status ==
                                    UserStatus.Active
                            )
                        ))
                    .Include(s => s.LostFound)
                        .ThenInclude(l => l!.Images)
                    .Include(s => s.LostFound)
                        .ThenInclude(l => l!.User)
                    .OrderByDescending(s => s.DateSaved)
                    .ToListAsync();


            // =====================================================
            // PETFEED
            // =====================================================

            var savedPetFeeds =
                await _context.SavedPetFeeds
                    .Where(s =>
                        s.MemberId == memberId)
                    .Include(s => s.PetFeed)
                        .ThenInclude(p => p!.Images)
                    .OrderByDescending(
                        s => s.SavedPetFeedId)
                    .ToListAsync();


            var model =
                new SavedItemsViewModel
                {
                    SavedListings = savedListings,
                    SavedLostFounds = savedLostFounds,
                    SavedPetFeeds = savedPetFeeds
                };


            return View(model);
        }


        // =========================================================
        // TOGGLE MARKETPLACE LISTING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleListing(int id)
        {
            var memberId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(memberId))
            {
                return Unauthorized();
            }


            // Make sure the Marketplace listing is still
            // publicly available and owned by an active member.
            var listingExists =
                await _context.Listings
                    .AnyAsync(l =>
                        l.ListingId == id
                        &&
                        l.Status ==
                            ListApprovalStatus.Approved
                        &&
                        l.ListStatus ==
                            ListingStatus.Pending
                        &&
                        l.Member != null
                        &&
                        l.Member.Status ==
                            UserStatus.Active);

            if (!listingExists)
            {
                return NotFound();
            }


            var existingSave =
                await _context.SavedListings
                    .FirstOrDefaultAsync(s =>
                        s.MemberId == memberId
                        &&
                        s.ListingId == id);


            // Already saved → unsave.
            if (existingSave != null)
            {
                _context.SavedListings.Remove(
                    existingSave
                );

                await _context.SaveChangesAsync();

                return Json(new
                {
                    saved = false
                });
            }


            // Not saved yet → save.
            var savedListing =
                new SavedListing
                {
                    MemberId = memberId,
                    ListingId = id,
                    DateSaved = PhilippineTime.Now
                };


            _context.SavedListings.Add(
                savedListing
            );

            await _context.SaveChangesAsync();


            return Json(new
            {
                saved = true
            });
        }


        // =========================================================
        // TOGGLE LOST & FOUND REPORT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLostFound(int id)
        {
            var memberId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(memberId))
            {
                return Unauthorized();
            }


            // Make sure the report is still public/active.
            //
            // Guest reports are allowed.
            // Registered reports require an active owner.
            var reportExists =
                await _context.LostFounds
                    .AnyAsync(l =>
                        l.LostFoundId == id
                        &&
                        l.Status ==
                            ApprovalStatus.Approved
                        &&
                        l.RStatus ==
                            ReportStatus.Active
                        &&
                        (
                            l.UserId == null
                            ||
                            (
                                l.User != null
                                &&
                                l.User.Status ==
                                    UserStatus.Active
                            )
                        ));

            if (!reportExists)
            {
                return NotFound();
            }


            var existingSave =
                await _context.SavedLostFounds
                    .FirstOrDefaultAsync(s =>
                        s.MemberId == memberId
                        &&
                        s.LostFoundId == id);


            if (existingSave != null)
            {
                _context.SavedLostFounds.Remove(
                    existingSave
                );

                await _context.SaveChangesAsync();

                return Json(new
                {
                    saved = false
                });
            }


            var savedLostFound =
                new SavedLostFound
                {
                    MemberId = memberId,
                    LostFoundId = id,
                    DateSaved = PhilippineTime.Now
                };


            _context.SavedLostFounds.Add(
                savedLostFound
            );

            await _context.SaveChangesAsync();


            return Json(new
            {
                saved = true
            });
        }


        // =========================================================
        // TOGGLE PETFEED POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePetFeed(int id)
        {
            var memberId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(memberId))
            {
                return Unauthorized();
            }


            var petFeedExists =
                await _context.PetFeeds
                    .AnyAsync(p =>
                        p.PetFeedId == id);

            if (!petFeedExists)
            {
                return NotFound();
            }


            var existingSave =
                await _context.SavedPetFeeds
                    .FirstOrDefaultAsync(s =>
                        s.MemberId == memberId
                        &&
                        s.PetFeedId == id);


            if (existingSave != null)
            {
                _context.SavedPetFeeds.Remove(
                    existingSave
                );

                await _context.SaveChangesAsync();

                return Json(new
                {
                    saved = false
                });
            }


            var savedPetFeed =
                new SavedPetFeed
                {
                    MemberId = memberId,
                    PetFeedId = id
                };


            _context.SavedPetFeeds.Add(
                savedPetFeed
            );

            await _context.SaveChangesAsync();


            return Json(new
            {
                saved = true
            });
        }
    }
}