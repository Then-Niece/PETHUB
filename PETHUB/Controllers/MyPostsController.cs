using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    [Authorize(Roles = "Member")]
    public class MyPostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        //Find the post.
        //Verify the logged-in user owns it.
        //Display the owner - specific view.
        public async Task<IActionResult> MarketplaceDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var listing = await _context.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.ListingId == id);

            if (listing == null)
                return NotFound();

            if (listing.MemberId != userId)
                return Forbid(); //Restrict non owner to access someone else's post

            return View(listing);
        }
        public async Task<IActionResult> LostFoundDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var report = await _context.LostFounds
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.LostFoundId == id);

            if (report == null)
                return NotFound();

            if (report.UserId != userId)
                return Forbid(); //Restrict non owner to access someone else's post

            return View(report);
        }
        public MyPostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
        public async Task<IActionResult> Delete(int id)
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
    }
}