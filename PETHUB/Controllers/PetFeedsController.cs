
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

public class PetFeedsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public PetFeedsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }


    //==========================================================
    //                        ADMIN
    //==========================================================


    // GET: PETFEEDS
    // petFeedType optionally filters the admin PetFeed management page
    // between Announcements and Pet Tips.
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(string? petFeedType)
    {
        // Get the currently logged-in administrator's ID.
        // This keeps the existing behavior where an admin does not
        // manage their own posts through this page.
        var userId = _userManager.GetUserId(User);

        // Start with PetFeed posts created by other administrators.
        // AsQueryable allows the optional type filter to be added
        // before the database query is executed.
        var query = _context.PetFeeds
            .Include(p => p.Admin)
            .Include(p => p.Images)
            .Where(p => p.AdminId != userId)
            .AsQueryable();

        // Apply the PetFeed type filter only when the administrator
        // selects a specific type.
        // Enum.TryParse converts "Announcement" or "PetTip" from the URL
        // into the PetFeedType enum used by the PetFeed model.
        if (!string.IsNullOrWhiteSpace(petFeedType) &&
            Enum.TryParse<PetFeedType>(
                petFeedType,
                out var selectedFeedType))
        {
            // EF Core translates this into a database WHERE condition.
            query = query.Where(p => p.Type == selectedFeedType);
        }

        // Keep the existing newest-first ordering.
        query = query.OrderByDescending(p => p.DateCreated);

        // Execute the final filtered query.
        var posts = await query.ToListAsync();

        return View(posts);
    }

    // GET: PETFEEDS/Details/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var petfeed = await _context.PetFeeds
            .Include(p => p.Admin)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.PetFeedId == id);

        if (petfeed == null)
        {
            return NotFound();
        }

        return View(petfeed);
    }

    // GET: PETFEEDS/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: PETFEEDS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(PetFeedViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var adminId = _userManager.GetUserId(User);

        if (adminId == null)
        {
            return Unauthorized();
        }

        var petFeed = new PetFeed
        {
            Title = model.Title,
            Content = model.Content,
            Type = model.Type,
            DateCreated = DateTime.Now,
            AdminId = adminId
        };

        _context.PetFeeds.Add(petFeed);
        await _context.SaveChangesAsync();


        // Save images if any were uploaded
        string? imagePath = null;

        if (model.Images != null && model.Images.Any(i => i.Length > 0))
        {
            var savedImages = await ImageHelper.SaveImagesAsync(
                model.Images,
                petFeed.PetFeedId,
                (id, path) => new PetFeedImage
                {
                    PetFeedId = id,
                    ImagePath = path
                },
                "petfeedimages"
            );

            _context.PetFeedImages.AddRange(savedImages);
            await _context.SaveChangesAsync();

            // Get the path of the first saved image for notification purposes
            imagePath = savedImages
            .FirstOrDefault()
            ?.ImagePath;
        }


        // SEND NOTIFICATION HERE
        var members = await _userManager.GetUsersInRoleAsync("Member");


        foreach (var member in members)
        {
            await _notificationService.CreateNotificationAsync(
                member.Id,

                petFeed.Type == PetFeedType.Announcement
                    ? NotificationType.NewAnnouncement
                    : NotificationType.NewPetTip,

                petFeed.Type == PetFeedType.Announcement
                    ? "New Announcement"
                    : "New Pet Tip",

                petFeed.Type == PetFeedType.Announcement
                    ? "A new announcement has been posted."
                    : "A new pet care tip has been posted.",

                imagePath,

                $"/PetFeeds/Feed?postId={petFeed.PetFeedId}#post-{petFeed.PetFeedId}",

                petFeedId: petFeed.PetFeedId
            );
        }


        return RedirectToAction(nameof(Index));
    }

    // GET: PETFEEDS/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var petfeed = await _context.PetFeeds
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.PetFeedId == id);

        if (petfeed == null)
        {
            return NotFound();
        }

        // Retrieve the currently logged-in administrator's ID.
        // This will be used to verify ownership of the selected post.
        var userId = _userManager.GetUserId(User);

        // Prevent administrators from editing another administrator's post
        // by manually changing the URL.
        if (petfeed.AdminId != userId)
        {
            return Forbid();
        }

        var model = new PetFeedViewModel
        {
            PetFeedId = petfeed.PetFeedId,
            Title = petfeed.Title,
            Content = petfeed.Content,
            Type = petfeed.Type,
            ExistingImages = petfeed.Images
        };


        return View(model);
    }

    // POST: PETFEEDS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, PetFeedViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingPetFeed = await _context.PetFeeds
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.PetFeedId == id);

        if (existingPetFeed == null)
        {
            return NotFound();
        }

        // Retrieve the currently logged-in administrator's ID.
        // This prevents direct POST requests from modifying another admin's post.
        var userId = _userManager.GetUserId(User);

        // Ensure only the owner of the PetFeed can save changes.
        if (existingPetFeed.AdminId != userId)
        {
            return Forbid();
        }

        existingPetFeed.Title = model.Title;
        existingPetFeed.Content = model.Content;
        existingPetFeed.Type = model.Type;


        if (model.Images != null && model.Images.Any(i => i.Length > 0))
        {
            var savedImages = await ImageHelper.SaveImagesAsync(
                model.Images,
                existingPetFeed.PetFeedId,
                (petFeedId, path) => new PetFeedImage
                {
                    PetFeedId = petFeedId,
                    ImagePath = path
                },
                "petfeedimages"
            );

            _context.PetFeedImages.AddRange(savedImages);
        }


        await _context.SaveChangesAsync();

        // Return the administrator to their personal My Posts page
        // after successfully updating the selected PetFeed.
        return RedirectToAction(
            "Index",
            "AdminMyPosts");
    }

    // GET: PETFEEDS/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var petfeed = await _context.PetFeeds
            .Include(p => p.Admin)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.PetFeedId == id);

        if (petfeed == null)
        {
            return NotFound();
        }

        // Retrieve the currently logged-in administrator's ID.
        var userId = _userManager.GetUserId(User);

        // Prevent administrators from accessing another administrator's post
        // by manually changing the URL.
        if (petfeed.AdminId != userId)
        {
            return Forbid();
        }

        return View(petfeed);
    }

    // POST: PETFEEDS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        // Retrieve the selected PetFeed together with its images.
        var petfeed = await _context.PetFeeds
            .Include(p => p.Admin)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.PetFeedId == id);

        // Return a 404 page if the post does not exist.
        if (petfeed == null)
        {
            return NotFound();
        }

        // Retrieve the currently logged-in administrator's ID.
        // This prevents direct POST requests from deleting another admin's post.
        var userId = _userManager.GetUserId(User);

        // Ensure only the owner of the PetFeed can permanently delete it.
        if (petfeed.AdminId != userId)
        {
            return Forbid();
        }

        // Delete all uploaded image files from wwwroot.
        if (petfeed.Images != null && petfeed.Images.Any())
        {
            foreach (var image in petfeed.Images)
            {
                var filepath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    image.ImagePath.TrimStart('/'));

                if (System.IO.File.Exists(filepath))
                {
                    System.IO.File.Delete(filepath);
                }

                _context.PetFeedImages.Remove(image);
            }
        }

        // Delete all notifications related to the PetFeed being deleted.
        var notifications = await _context.Notifications
            .Where(n => n.PetFeedId == petfeed.PetFeedId)
            .ToListAsync();

        _context.Notifications.RemoveRange(notifications);

        // Remove the PetFeed record from the database.
        _context.PetFeeds.Remove(petfeed);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveImage(int id)
    {
        var petFeedId = await ImageHelper.RemoveImageAsync(
            _context,
            _context.PetFeedImages,
            id,
            img => img.ImagePath,   // how to get file path
            img => img.PetFeedId    // how to get parent ID
        );

        if (petFeedId == null)
        {
            return NotFound();
        }

        return RedirectToAction("Edit", new { id = petFeedId });
    }



    private bool PetFeedExists(int? petfeedid)
    {
        return _context.PetFeeds.Any(e => e.PetFeedId == petfeedid);
    }


    //==========================================================
    //                        MEMBER
    //==========================================================

    // MEMBER FEED
    // petFeedType optionally filters the feed between Announcements and Pet Tips.
    [AllowAnonymous]
    public async Task<IActionResult> Feed(
        int? postId,
        string? petFeedType)
    {
        // Get the current user's ID so the view can determine
        // whether each member has already pawed a post.
        var userId = _userManager.GetUserId(User);

        // Start with all PetFeed posts and their related data.
        // AsQueryable allows the optional feed-type filter to be applied
        // before the database query is executed.
        var query = _context.PetFeeds
            .Include(p => p.Images)
            .Include(p => p.Paws)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Member)
            .AsSplitQuery()
            .AsQueryable();

        // Apply the PetFeed type filter only when the user selected
        // a specific feed type.
        // Enum.TryParse converts "Announcement" or "PetTip" from the URL
        // into the PetFeedType enum used by the database model.
        if (!string.IsNullOrWhiteSpace(petFeedType) &&
            Enum.TryParse<PetFeedType>(
                petFeedType,
                out var selectedFeedType))
        {
            // EF Core translates this comparison into a database WHERE condition.
            query = query.Where(p => p.Type == selectedFeedType);
        }

        // Apply the existing newest-first ordering.
        query = query
            .OrderByDescending(p => p.DateCreated);

        // If the visitor is not authenticated, keep the existing
        // limit of 10 posts for the public feed.
        if (!User.Identity.IsAuthenticated)
        {
            query = query.Take(10);
        }

        // Execute the final filtered query.
        var posts = await query.ToListAsync();

        // Convert database entities into the Feed ViewModels used by the view.
        var model = posts.Select(p => new PetFeedFeedViewModel
        {
            PetFeedId = p.PetFeedId,
            Title = p.Title,
            Content = p.Content,
            DateCreated = p.DateCreated,
            Type = p.Type,

            Images = p.Images,

            // Count the number of paws for this post.
            PawCount = p.Paws.Count(),

            // Check whether the current member has pawed this post.
            IsPawed = userId != null &&
                      p.Paws.Any(x => x.MemberId == userId),

            // Count comments without changing the existing comment behavior.
            CommentCount = p.Comments.Count(),

            Comments = p.Comments,

            // Highlight the requested post when postId is supplied.
            IsHighlighted = postId == p.PetFeedId

        }).ToList();

        // Send the filtered PetFeed posts to the existing Feed view.
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Paw(int id)
    {
        var userId = _userManager.GetUserId(User);


        var alreadyPawed = await _context.PetFeedPaws
            .AnyAsync(p =>
                p.PetFeedId == id &&
                p.MemberId == userId);


        if (!alreadyPawed)
        {
            var paw = new PetFeedPaw
            {
                PetFeedId = id,
                MemberId = userId,
                DatePawed = DateTime.Now
            };


            _context.PetFeedPaws.Add(paw);

            await _context.SaveChangesAsync();
        }


        return RedirectToAction(nameof(Feed));
    }



    [HttpPost]
    [Authorize(Roles = "Member")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpaw(int id)
    {
        var userId = _userManager.GetUserId(User);


        var paw = await _context.PetFeedPaws
            .FirstOrDefaultAsync(p =>
                p.PetFeedId == id &&
                p.MemberId == userId);


        if (paw != null)
        {
            _context.PetFeedPaws.Remove(paw);

            await _context.SaveChangesAsync();
        }


        return RedirectToAction(nameof(Feed));
    }


    [HttpPost]
    [Authorize(Roles = "Member")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction(nameof(Feed));
        }

        var userId = _userManager.GetUserId(User);

        var comment = new PetFeedComment
        {
            PetFeedId = id,
            MemberId = userId,
            Content = content,
            DatePosted = DateTime.Now
        };

        _context.PetFeedComments.Add(comment);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Feed));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var userId = _userManager.GetUserId(User);

        var comment = await _context.PetFeedComments
            .FirstOrDefaultAsync(c => c.CommentId == id);


        if (comment == null)
        {
            return NotFound();
        }


        // Member can only delete their own comment
        if (!User.IsInRole("Admin") && comment.MemberId != userId)
        {
            return Forbid();
        }


        _context.PetFeedComments.Remove(comment);

        await _context.SaveChangesAsync();


        return RedirectToAction(nameof(Feed));
    }

}
