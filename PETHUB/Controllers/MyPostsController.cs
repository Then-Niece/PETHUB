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
    [Authorize(Roles = "Member")]
    public class MyPostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProfileService _profileService;

        public MyPostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProfileService profileService)
        {
            _context = context;
            _userManager = userManager;
            _profileService = profileService;
        }

        //Find the post.
        //Verify the logged-in user owns it.
        //Display the owner - specific view.
        public async Task<IActionResult> MarketplaceDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var listing = await _context.Listings
                .Include(l => l.Member)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.ListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            if (listing.MemberId != userId)
            {
                return Forbid(); //Restrict non owner to access someone else's post
            }

            return View(listing);
        }
        public async Task<IActionResult> LostFoundDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var report = await _context.LostFounds
                .Include(r => r.User)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.LostFoundId == id);

            if (report == null)
            {
                return NotFound();
            }

            if (report.UserId != userId)
            {
                return Forbid(); //Restrict non owner to access someone else's post
            }

            return View(report);
        }


        // Displays all posts created by the logged-in member.
        public async Task<IActionResult> Index()
        {
            // Get the current user's ID.
            var userId = _userManager.GetUserId(User);

            // Load the member's marketplace listings.
            var listings = await _context.Listings
                .Include(l => l.Images)
                .Where(l => l.MemberId == userId)
                .ToListAsync();

            // Load the member's lost and found reports.
            var reports = await _context.LostFounds
                .Include(r => r.Images)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var model = new MyPostsViewModel();

            // Retrieve the logged-in user.
            var user = await _userManager.GetUserAsync(User);

            // Build the reusable profile information.
            model.Profile = await _profileService.BuildProfileViewModelAsync(user);

            // Add marketplace listings.
            foreach (var listing in listings)
            {
                model.Posts.Add((listing, null));
            }

            // Add lost and found reports.
            foreach (var report in reports)
            {
                model.Posts.Add((null, report));
            }

            // Sort posts by newest first.
            model.Posts = model.Posts
                .OrderByDescending(post =>
                    post.Listing != null
                        ? post.Listing.DatePosted
                        : post.Report!.DateReported)
                .ToList();

            return View(model);
        }

        //Delete Functions
        // POST: MyPosts/DeleteMarketplace/5
        // Deletes a marketplace listing owned by the currently logged-in member.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMarketplace(int id)
        {
            // Retrieve the currently logged-in user's ID.
            var userId = _userManager.GetUserId(User);

            // Retrieve the listing together with its images.
            var listing = await _context.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.ListingId == id);

            // Return a 404 page if the listing does not exist.
            if (listing == null)
            {
                return NotFound();
            }

            // Ensure that only the owner of the listing can delete it.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }

            // Delete image files from wwwroot.
            if (listing.Images != null && listing.Images.Any())
            {
                foreach (var image in listing.Images)
                {
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        image.ImagePath.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    _context.ListingImages.Remove(image);
                }
            }

            // Delete the listing from the database.
            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();

            // Return the user to the My Posts page.
            return RedirectToAction(nameof(Index));
        }

        // POST: MyPosts/DeleteLostFound/5
        // Deletes a lost and found report owned by the currently logged-in member.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLostFound(int id)
        {
            // Retrieve the currently logged-in user's ID.
            var userId = _userManager.GetUserId(User);

            // Retrieve the report together with its images.
            var report = await _context.LostFounds
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.LostFoundId == id);

            // Return a 404 page if the report does not exist.
            if (report == null)
            {
                return NotFound();
            }

            // Ensure only the owner can delete it.
            if (report.UserId != userId)
            {
                return Forbid();
            }

            // Delete image files from wwwroot.
            if (report.Images != null && report.Images.Any())
            {
                foreach (var image in report.Images)
                {
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        image.ImagePath.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    _context.LostFoundImages.Remove(image);
                }
            }

            // Delete the report.
            _context.LostFounds.Remove(report);
            await _context.SaveChangesAsync();

            // Return to My Posts.
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            // Retrieve the currently logged-in user's ID.
            var userId = _userManager.GetUserId(User);

            // Retrieve the listing.
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.ListingId == id);

            // Ensure the listing exists.
            if (listing == null)
            {
                return NotFound();
            }

            // Ensure only the owner can perform this action.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }

            // Only approved listings may be marked as sold.
            if (listing.Status != ListApprovalStatus.Approved)
            {
                return Forbid();
            }

            // Only listings that are still pending may be updated.
            if (listing.ListStatus != ListingStatus.Pending)
            {
                return Forbid();
            }

            listing.ListStatus = ListingStatus.Sold;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(MarketplaceDetails),
                new { id = listing.ListingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsAdopted(int id)
        {
            // Retrieve the currently logged-in user's ID.
            var userId = _userManager.GetUserId(User);

            // Retrieve the listing.
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.ListingId == id);

            // Ensure the listing exists.
            if (listing == null)
            {
                return NotFound();
            }

            // Ensure only the owner can perform this action.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }

            // Only approved listings may be marked as adopted.
            if (listing.Status != ListApprovalStatus.Approved)
            {
                return Forbid();
            }

            // Only listings that are still pending may be updated.
            if (listing.ListStatus != ListingStatus.Pending)
            {
                return Forbid();
            }

            listing.ListStatus = ListingStatus.Adopted;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(MarketplaceDetails),
                new { id = listing.ListingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReportResolved(int id)
        {
            // Retrieve the currently logged-in user's ID.
            var userId = _userManager.GetUserId(User);

            // Retrieve the report.
            var report = await _context.LostFounds
                .FirstOrDefaultAsync(r => r.LostFoundId == id);

            // Ensure the report exists.
            if (report == null)
            {
                return NotFound();
            }

            // Ensure only the owner can perform this action.
            if (report.UserId != userId)
            {
                return Forbid();
            }

            // Only approved reports may be resolved.
            if (report.Status != ApprovalStatus.Approved)
            {
                return Forbid();
            }

            // Only active reports may be resolved.
            if (report.RStatus != ReportStatus.Active)
            {
                return Forbid();
            }

            report.RStatus = ReportStatus.Resolved;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(LostFoundDetails),
                new { id = report.LostFoundId });
        }
    }
}