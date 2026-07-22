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
    }
}