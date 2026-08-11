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
    /// <summary>
    /// Personal dashboard where administrators can view
    /// and manage only the PetFeed posts they created.
    /// </summary>

    // Restricts access to authenticated users
    // who are assigned the Admin role.
    [Authorize(Roles = "Admin")]
    public class AdminMyPostsController : Controller
    {
        // Provides access to the application's database.
        // Used to retrieve the logged-in admin's PetFeed posts.
        private readonly ApplicationDbContext _context;

        // Provides information about the currently authenticated user.
        // We use this to retrieve the logged-in admin's User ID.
        private readonly UserManager<ApplicationUser> _userManager;

        // Builds the reusable profile information displayed
        // on pages such as the profile card/sidebar.
        private readonly AdminIProfileService _profileService;

        // Receives the required services through Dependency Injection.
        // ASP.NET Core automatically supplies these services at runtime.
        public AdminMyPostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AdminIProfileService profileService)
        {
            _context = context;
            _userManager = userManager;
            _profileService = profileService;
        }

        // Displays all PetFeed posts created by the currently logged-in administrator.
        public async Task<IActionResult> Index()
        {
            // Retrieve the currently authenticated administrator.
            // If no user is found, return an Unauthorized response.
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            // Retrieve all PetFeed posts that belong only to the logged-in admin.
            // Include related data so the view can display images,
            // paw counts, and comment counts without additional queries.
            var posts = await _context.PetFeeds
                .Include(p => p.Images)
                .Include(p => p.Paws)
                .Include(p => p.Comments)
                .Where(p => p.AdminId == user.Id)
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync();

            // Create the page ViewModel.
            var model = new AdminMyPostsViewModel();

            // Build the reusable profile information.
            // This keeps the profile card/sidebar consistent with other modules.
            model.Profile = await _profileService.BuildAdminProfileViewModelAsync(user);

            // Store the administrator's PetFeed posts.
            model.Posts = posts;

            return View(model);
        }

        // Displays the complete details of a PetFeed post
        // created by the currently logged-in administrator.
        public async Task<IActionResult> Details(int id)
        {
            // Retrieve the currently logged-in administrator's ID.
            // This will be used to verify ownership of the selected post.
            var userId = _userManager.GetUserId(User);

            // Retrieve the selected PetFeed together with all related data
            // required by the Details page.
            var post = await _context.PetFeeds
                .Include(p => p.Admin)
                .Include(p => p.Images)
                .Include(p => p.Paws)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Member)
                .FirstOrDefaultAsync(p => p.PetFeedId == id);

            // Return a 404 page if the post does not exist.
            if (post == null)
            {
                return NotFound();
            }

            // Prevent administrators from viewing another administrator's post
            // through manual URL manipulation.
            if (post.AdminId != userId)
            {
                return Forbid();
            }

            return View(post);
        }

        // Deletes a PetFeed post owned by the currently logged-in administrator.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Retrieve the currently logged-in administrator's ID.
            var userId = _userManager.GetUserId(User);

            // Retrieve the selected PetFeed together with its images.
            var petfeed = await _context.PetFeeds
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.PetFeedId == id);

            // Return a 404 page if the post does not exist.
            if (petfeed == null)
            {
                return NotFound();
            }

            // Ensure only the owner can delete this post.
            if (petfeed.AdminId != userId)
            {
                return Forbid();
            }

            // Remove all related image records.
            // (Image deletion can be improved later through ImageHelper.)
            if (petfeed.Images != null && petfeed.Images.Any())
            {
                _context.PetFeedImages.RemoveRange(petfeed.Images);
            }

            // Delete all notifications related to the PetFeed being deleted.
            var notifications = await _context.Notifications
                .Where(n => n.PetFeedId == petfeed.PetFeedId)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);

            // Remove the PetFeed record.
            _context.PetFeeds.Remove(petfeed);

            await _context.SaveChangesAsync();

            // Return to the administrator's personal My Posts page.
            return RedirectToAction(nameof(Index));
        }
    }
}