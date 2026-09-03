using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    // Restricts appeal creation to authenticated Members.
    // Only Members can appeal the removal of their own posts.
    [Authorize(Roles = "Member")]
    public class AppealsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppealsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // Displays the Appeal form for a specific removed post.
        [HttpGet]
        public async Task<IActionResult> Create(string type, int id)
        {
            // Gets the Identity ID of the currently authenticated Member.
            // This prevents Members from appealing another Member's post.
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Reject invalid post IDs before querying the database.
            if (id <= 0)
            {
                return NotFound();
            }


            // =========================================================
            // MARKETPLACE LISTING
            // =========================================================

            if (string.Equals(type, "listing", StringComparison.OrdinalIgnoreCase))
            {
                // Retrieve the existing removed Marketplace listing belonging
                // to the currently authenticated Member.
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l =>
                        l.ListingId == id &&
                        l.MemberId == userId &&
                        l.Status == ListApprovalStatus.Removed);

                if (listing == null)
                {
                    return NotFound();
                }

                // Prevent multiple active appeals for the same removed listing.
                var existingAppeal = await _context.Appeals
                    .AnyAsync(a =>
                        a.ListingId == listing.ListingId &&
                        a.Status == AppealStatus.Pending);

                if (existingAppeal)
                {

                    TempData["InfoMessage"] = "You already have a pending appeal for this post.";

                    // The Member already has an appeal waiting for Admin review.
                    // Return them to the existing Removed Post Details page.
                    return RedirectToAction(
                        "Details",
                        "RemovedPosts",
                        new
                        {
                            type = "listing",
                            id = listing.ListingId
                        });
                }

                // Build the form model using information from the existing post.
                // The post itself is not copied or recreated.
                var model = new AppealViewModel
                {
                    PostType = "listing",
                    PostId = listing.ListingId,
                    PostTitle = listing.Title
                };

                return View(model);
            }


            // =========================================================
            // LOST & FOUND POST
            // =========================================================

            if (string.Equals(type, "lostfound", StringComparison.OrdinalIgnoreCase))
            {
                // Retrieve the existing removed Lost & Found post belonging
                // to the currently authenticated Member.
                var lostFound = await _context.LostFounds
                    .FirstOrDefaultAsync(l =>
                        l.LostFoundId == id &&
                        l.UserId == userId &&
                        l.Status == ApprovalStatus.Removed);

                if (lostFound == null)
                {
                    return NotFound();
                }

                // Prevent multiple active appeals for the same removed post.
                var existingAppeal = await _context.Appeals
                    .AnyAsync(a =>
                        a.LostFoundId == lostFound.LostFoundId &&
                        a.Status == AppealStatus.Pending);

                if (existingAppeal)
                {
                    // The Member already has an appeal waiting for Admin review.
                    return RedirectToAction(
                        "Details",
                        "RemovedPosts",
                        new
                        {
                            type = "lostfound",
                            id = lostFound.LostFoundId
                        });
                }

                // Build the form model using the existing Lost & Found post.
                var model = new AppealViewModel
                {
                    PostType = "lostfound",
                    PostId = lostFound.LostFoundId,
                    PostTitle = lostFound.Title
                };

                return View(model);
            }


            // Reject unsupported post types.
            return NotFound();
        }


        // Saves a Member's appeal for an existing removed post.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppealViewModel model)
        {
            // Gets the current Member's Identity ID.
            // This is used to verify ownership again on POST instead of trusting
            // the values submitted by the browser.
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Validate the appeal message before creating the database record.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Reject invalid IDs before querying the database.
            if (model.PostId <= 0)
            {
                return NotFound();
            }


            // =========================================================
            // MARKETPLACE LISTING
            // =========================================================

            if (string.Equals(model.PostType, "listing", StringComparison.OrdinalIgnoreCase))
            {
                // Retrieve the existing removed Marketplace listing.
                // Ownership and Removed status are checked again during POST.
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l =>
                        l.ListingId == model.PostId &&
                        l.MemberId == userId &&
                        l.Status == ListApprovalStatus.Removed);

                if (listing == null)
                {
                    return NotFound();
                }

                // Prevent duplicate Pending appeals for the same listing.
                var existingAppeal = await _context.Appeals
                    .AnyAsync(a =>
                        a.ListingId == listing.ListingId &&
                        a.Status == AppealStatus.Pending);

                if (existingAppeal)
                {
                    return RedirectToAction(
                        "Details",
                        "RemovedPosts",
                        new
                        {
                            type = "listing",
                            id = listing.ListingId
                        });
                }

                // Create an Appeal that points to the existing Listing.
                // No duplicate Listing or post content is created.
                var appeal = new Appeal
                {
                    MemberId = userId,
                    ListingId = listing.ListingId,
                    LostFoundId = null,
                    AppealMessage = model.AppealMessage,
                    Status = AppealStatus.Pending,
                    DateCreated = DateTime.Now
                };

                _context.Appeals.Add(appeal);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your appeal has been submitted successfully.";

                // Return to the same Removed Post Details page after submission.
                return RedirectToAction(
                    "Details",
                    "RemovedPosts",
                    new
                    {
                        type = "listing",
                        id = listing.ListingId
                    });
            }


            // =========================================================
            // LOST & FOUND POST
            // =========================================================

            if (string.Equals(model.PostType, "lostfound", StringComparison.OrdinalIgnoreCase))
            {
                // Retrieve the existing removed Lost & Found post.
                // Ownership and Removed status are checked again during POST.
                var lostFound = await _context.LostFounds
                    .FirstOrDefaultAsync(l =>
                        l.LostFoundId == model.PostId &&
                        l.UserId == userId &&
                        l.Status == ApprovalStatus.Removed);

                if (lostFound == null)
                {
                    return NotFound();
                }

                // Prevent duplicate Pending appeals for the same Lost & Found post.
                var existingAppeal = await _context.Appeals
                    .AnyAsync(a =>
                        a.LostFoundId == lostFound.LostFoundId &&
                        a.Status == AppealStatus.Pending);

                if (existingAppeal)
                {
                    TempData["InfoMessage"] = "You already have a pending appeal for this post.";

                    return RedirectToAction(
                        "Details",
                        "RemovedPosts",
                        new
                        {
                            type = "lostfound",
                            id = lostFound.LostFoundId
                        });
                }

                // Create an Appeal that points to the existing Lost & Found post.
                var appeal = new Appeal
                {
                    MemberId = userId,
                    ListingId = null,
                    LostFoundId = lostFound.LostFoundId,
                    AppealMessage = model.AppealMessage,
                    Status = AppealStatus.Pending,
                    DateCreated = DateTime.Now
                };

                _context.Appeals.Add(appeal);

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] = "Your appeal has been submitted successfully.";

                // Return to the same Removed Post Details page after submission.
                return RedirectToAction(
                    "Details",
                    "RemovedPosts",
                    new
                    {
                        type = "lostfound",
                        id = lostFound.LostFoundId
                    });
            }


            // Reject unsupported post types.
            return NotFound();
        }
    }
}