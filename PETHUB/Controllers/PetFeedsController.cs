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

    public PetFeedsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }


    //==========================================================
    //                        ADMIN
    //==========================================================


    // GET: PETFEEDS
    // Displays administrator-created PetFeed posts.
    // The optional petFeedType parameter filters Announcements or Pet Tips.
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(string? petFeedType)
    {
        // Get the currently logged-in administrator's ID.
        // This preserves the existing behavior of identifying the current admin.
        var userId = _userManager.GetUserId(User);

        // Start with PetFeed records and load the admin and images.
        // AsQueryable allows the optional filter to be applied before execution.
        var query = _context.PetFeeds
            .Include(p => p.Admin)
            .Include(p => p.Images)
            .AsQueryable();

        // Apply the type filter only when a valid PetFeedType was supplied.
        if (!string.IsNullOrWhiteSpace(petFeedType) &&
            Enum.TryParse<PetFeedType>(
                petFeedType,
                out var selectedFeedType))
        {
            // EF Core translates this into a SQL WHERE condition.
            query = query.Where(p => p.Type == selectedFeedType);
        }

        // Keep the existing newest-first ordering.
        query = query.OrderByDescending(p => p.DateCreated);

        // Execute the query and load the administrator's PetFeed records.
        var posts = await query.ToListAsync();

        return View(posts);
    }


    // GET: PETFEEDS/Details/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(int? id)
    {
        // Return 404 if no ID was supplied.
        if (id == null)
        {
            return NotFound();
        }

        // Retrieve the selected PetFeed and its related administrator/images.
        var petfeed = await _context.PetFeeds
            .Include(p => p.Admin)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.PetFeedId == id);

        // Return 404 if the PetFeed does not exist.
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(PetFeedViewModel model)
    {
        // Return to the form when validation fails.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Get the administrator creating the PetFeed.
        var adminId = _userManager.GetUserId(User);

        // An authenticated admin must have an Identity ID.
        if (adminId == null)
        {
            return Unauthorized();
        }

        // Create the new PetFeed entity using the submitted form data.
        var petFeed = new PetFeed
        {
            Title = model.Title,
            Content = model.Content,
            Type = model.Type,
            DateCreated = DateTime.Now,
            AdminId = adminId
        };

        // Add the new PetFeed to EF Core.
        _context.PetFeeds.Add(petFeed);

        // Save first so PetFeedId becomes available for its images.
        await _context.SaveChangesAsync();


        // Store the first uploaded image path for notification purposes.
        string? imagePath = null;

        // Save uploaded images when the administrator provided any files.
        if (model.Images != null &&
            model.Images.Any(i => i.Length > 0))
        {
            // ImageHelper handles saving the physical files and
            // creates PetFeedImage entities using the supplied factory.
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

            // Add all saved image records to the database.
            _context.PetFeedImages.AddRange(savedImages);

            await _context.SaveChangesAsync();

            // Use the first image for the member notification.
            imagePath = savedImages
                .FirstOrDefault()
                ?.ImagePath;
        }


        // Retrieve all members so each member can receive the
        // existing new Announcement / Pet Tip notification.
        var members = await _userManager.GetUsersInRoleAsync("Member");


        foreach (var member in members)
        {
            // Create the existing notification for the newly created PetFeed.
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
        // Return 404 when no ID was supplied.
        if (id == null)
        {
            return NotFound();
        }

        // Retrieve the PetFeed and its existing images.
        var petfeed = await _context.PetFeeds
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.PetFeedId == id);

        // Return 404 when the PetFeed does not exist.
        if (petfeed == null)
        {
            return NotFound();
        }

        // Get the current administrator's ID.
        var userId = _userManager.GetUserId(User);

        // Prevent an administrator from editing another admin's post.
        if (petfeed.AdminId != userId)
        {
            return Forbid();
        }

        // Populate the existing edit ViewModel.
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
    public async Task<IActionResult> Edit(
        int id,
        PetFeedViewModel model)
    {
        // Return to the edit form when validation fails.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Retrieve the selected PetFeed and its existing images.
        var existingPetFeed = await _context.PetFeeds
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.PetFeedId == id);

        // Return 404 if it does not exist.
        if (existingPetFeed == null)
        {
            return NotFound();
        }

        // Get the current administrator's ID.
        var userId = _userManager.GetUserId(User);

        // Only the administrator who owns the PetFeed can modify it.
        if (existingPetFeed.AdminId != userId)
        {
            return Forbid();
        }

        // Update the editable PetFeed properties.
        existingPetFeed.Title = model.Title;
        existingPetFeed.Content = model.Content;
        existingPetFeed.Type = model.Type;


        // Save any newly uploaded images.
        if (model.Images != null &&
            model.Images.Any(i => i.Length > 0))
        {
            // ImageHelper saves the files and creates the related entities.
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

            // Add the newly created image records.
            _context.PetFeedImages.AddRange(savedImages);
        }


        // Save all changes.
        await _context.SaveChangesAsync();

        // Return the administrator to their personal My Posts page.
        return RedirectToAction(
            "Index",
            "AdminMyPosts");
    }


    // GET: PETFEEDS/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        // Return 404 if no ID was supplied.
        if (id == null)
        {
            return NotFound();
        }

        // Retrieve the selected PetFeed and its related data.
        var petfeed = await _context.PetFeeds
            .Include(p => p.Admin)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.PetFeedId == id);

        // Return 404 if the PetFeed does not exist.
        if (petfeed == null)
        {
            return NotFound();
        }

        // Get the current administrator's ID.
        var userId = _userManager.GetUserId(User);

        // Prevent an administrator from accessing another admin's post.
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
        // Retrieve the selected PetFeed with its images.
        var petfeed = await _context.PetFeeds
            .Include(p => p.Admin)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.PetFeedId == id);

        // Return 404 if the PetFeed does not exist.
        if (petfeed == null)
        {
            return NotFound();
        }

        // Get the current administrator's ID.
        var userId = _userManager.GetUserId(User);

        // Only the owner can delete the PetFeed.
        if (petfeed.AdminId != userId)
        {
            return Forbid();
        }

        // Delete all physical image files and their database records.
        if (petfeed.Images != null &&
            petfeed.Images.Any())
        {
            foreach (var image in petfeed.Images)
            {
                // Convert the stored web path into a physical wwwroot path.
                var filepath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    image.ImagePath.TrimStart('/'));

                // Delete the physical file when it exists.
                if (System.IO.File.Exists(filepath))
                {
                    System.IO.File.Delete(filepath);
                }

                // Remove the image database record.
                _context.PetFeedImages.Remove(image);
            }
        }

        // Remove notifications associated with this PetFeed.
        var notifications = await _context.Notifications
            .Where(n => n.PetFeedId == petfeed.PetFeedId)
            .ToListAsync();

        _context.Notifications.RemoveRange(notifications);

        // Remove the PetFeed itself.
        _context.PetFeeds.Remove(petfeed);

        // Save the deletion.
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    // Removes one image from an existing PetFeed.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveImage(int id)
    {
        // ImageHelper removes the physical file and database entity.
        var petFeedId = await ImageHelper.RemoveImageAsync(
            _context,
            _context.PetFeedImages,
            id,
            img => img.ImagePath,
            img => img.PetFeedId
        );

        // Return 404 when the image does not exist.
        if (petFeedId == null)
        {
            return NotFound();
        }

        // Return to the PetFeed edit page.
        return RedirectToAction(
            "Edit",
            new { id = petFeedId });
    }


    // Checks whether a PetFeed exists.
    private bool PetFeedExists(int? petfeedid)
    {
        return _context.PetFeeds
            .Any(e => e.PetFeedId == petfeedid);
    }


    //==========================================================
    //                        MEMBER
    //==========================================================


    // Displays the PetFeed to both anonymous visitors and Members.
    //
    // Anonymous visitors:
    // - Can see administrator-created PetFeed posts.
    // - Cannot see Marketplace or Lost & Found posts.
    //
    // Members:
    // - Can see administrator-created PetFeed posts.
    // - Can see approved Marketplace listings from their own City,
    //   excluding their own listings.
    // - Can see approved and active Lost & Found reports from their own City,
    //   excluding their own reports.
    // When a Member opens a fresh PetFeed page, a new random feed seed is created.
    // The seed is reused during pagination so the feed order does not reshuffle.
    [AllowAnonymous]
    public async Task<IActionResult> Feed(
        int? postId,
        long? feedSeed)
    {

        // Retrieve announcements created within the last 24 hours.
        // Announcements are public administrator content, so anonymous visitors
        // can also see the announcement carousel.
        var announcementCutoff = DateTime.Now.AddDays(-1);

        var announcements = await _context.PetFeeds
            .AsNoTracking()
            .Where(p =>
                p.Type == PetFeedType.Announcement &&
                p.DateCreated >= announcementCutoff &&
                p.DateCreated <= DateTime.Now)
            .Include(p => p.Images)
            .OrderByDescending(p => p.DateCreated)
            .ToListAsync();

        // Make the announcements available to Feed.cshtml.
        ViewData["Announcements"] = announcements;

        // Get the currently authenticated user's Identity ID.
        // This returns null when the visitor is anonymous.
        var userId = _userManager.GetUserId(User);

        // ----------------------------------------------------------
        // ANONYMOUS VISITOR OR NON-MEMBER
        // ----------------------------------------------------------
        //
        // Anonymous users must continue seeing PetFeed content only.
        // We also keep this behavior for Admin users because the combined
        // location-based feed is intended for Members.
        if (!User.Identity?.IsAuthenticated == true ||
            !User.IsInRole("Member"))
        {
            // Only Pet Tips remain in the normal anonymous feed.
            // Announcements are displayed separately in the announcement carousel,
            // so they must not appear again in the normal feed.
            var anonymousQuery = _context.PetFeeds
                .Where(p => p.Type == PetFeedType.PetTip)
                .Include(p => p.Images)
                .Include(p => p.Paws)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Member)
                .AsSplitQuery()
                .AsQueryable();

            // Preserve the existing newest-first ordering.
            anonymousQuery = anonymousQuery
                .OrderByDescending(p => p.DateCreated);

            // Preserve the existing anonymous limit of 10 posts.
            anonymousQuery = anonymousQuery.Take(10);

            // Execute the PetFeed-only query.
            var anonymousPosts = await anonymousQuery.ToListAsync();

            // Convert the existing PetFeed records to the same ViewModel
            // used by the combined Member feed.
            var anonymousModel = anonymousPosts
                .Select(p => new PetFeedFeedViewModel
                {
                    // Identify this item as administrator-created PetFeed.
                    ContentType = PetFeedContentType.PetFeed,

                    // PetFeed's original ID.
                    PetFeedId = p.PetFeedId,

                    // ContentId uses the same ID for PetFeed.
                    ContentId = p.PetFeedId,

                    Title = p.Title,

                    Content = p.Content,

                    DateCreated = p.DateCreated,

                    Type = p.Type,

                    // Keep the existing PetFeed image relationship.
                    Images = p.Images,

                    // Also populate the common image-path list used
                    // by the combined feed rendering.
                    ImagePaths = p.Images?
                        .Select(i => i.ImagePath)
                        .ToList()
                        ?? new List<string>(),

                    // Preserve existing comment behavior.
                    Comments = p.Comments
                        ?? new List<PetFeedComment>(),

                    CommentCount = p.Comments?.Count ?? 0,

                    // Preserve existing paw behavior.
                    PawCount = p.Paws?.Count ?? 0,

                    IsPawed =
                        userId != null &&
                        p.Paws != null &&
                        p.Paws.Any(
                            x => x.MemberId == userId),

                    // Preserve the existing highlighted-post behavior.
                    IsHighlighted =
                        postId == p.PetFeedId
                })
                .ToList();

            // Anonymous/non-member visitors receive only PetFeed.
            return View(anonymousModel);
        }


        // ----------------------------------------------------------
        // MEMBER COMBINED FEED
        // ----------------------------------------------------------

        // The combined feed requires the authenticated Member's ID.
        if (userId == null)
        {
            // This should not normally occur because the condition above
            // handles anonymous visitors, but Unauthorized is safer than
            // attempting a location-based query without an Identity ID.
            return Unauthorized();
        }

        // Retrieve the complete ApplicationUser so the combined feed
        // can use the City stored on the member's account.
        var user = await _userManager.FindByIdAsync(userId);

        // Temporarily print the Member's City so we can compare it
        // against the City values stored in Marketplace and Lost & Found.
        Console.WriteLine($"MEMBER CITY: '{user?.City}'");

        // Temporarily print Marketplace records and their filtering values.
        // This is only for debugging and will be removed afterward.
        var debugListings = await _context.Listings
            .Select(l => new
            {
                l.ListingId,
                l.City,
                l.MemberId,
                l.Status,
                l.ListStatus
            })
            .ToListAsync();

        foreach (var listing in debugListings)
        {
            // Display the values that determine Marketplace eligibility.
            Console.WriteLine(
                $"LISTING {listing.ListingId}: " +
                $"City='{listing.City}', " +
                $"MemberId='{listing.MemberId}', " +
                $"Status='{listing.Status}', " +
                $"ListStatus='{listing.ListStatus}'");
        }

        // Temporarily print Lost & Found records and their filtering values.
        var debugLostFound = await _context.LostFounds
            .Select(l => new
            {
                l.LostFoundId,
                l.City,
                l.UserId,
                l.Status,
                l.RStatus
            })
            .ToListAsync();

        foreach (var report in debugLostFound)
        {
            // Display the values that determine Lost & Found eligibility.
            Console.WriteLine(
                $"LOSTFOUND {report.LostFoundId}: " +
                $"City='{report.City}', " +
                $"UserId='{report.UserId}', " +
                $"Status='{report.Status}', " +
                $"RStatus='{report.RStatus}'");
        }

        // Return 401 when the Identity record cannot be found.
        if (user == null)
        {
            return Unauthorized();
        }

        // The Marketplace and Lost & Found filters require a City.
        // PetFeed itself does not require a City.
        if (string.IsNullOrWhiteSpace(user.City))
        {
            // Only Pet Tips remain in the fallback normal feed.
            // Announcements are already displayed separately in the carousel.
            var petFeedOnlyQuery = _context.PetFeeds
                .Where(p => p.Type == PetFeedType.PetTip)
                .Include(p => p.Images)
                .Include(p => p.Paws)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Member)
                .AsSplitQuery()
                .AsQueryable();

            // Preserve newest-first ordering.
            petFeedOnlyQuery = petFeedOnlyQuery
                .OrderByDescending(p => p.DateCreated)
                .Take(10);

            // Execute the fallback PetFeed-only query.
            var petFeedOnlyPosts =
                await petFeedOnlyQuery.ToListAsync();

            // Convert the PetFeed records to the existing ViewModel.
            var petFeedOnlyModel = petFeedOnlyPosts
                .Select(p => new PetFeedFeedViewModel
                {
                    ContentType = PetFeedContentType.PetFeed,

                    PetFeedId = p.PetFeedId,

                    ContentId = p.PetFeedId,

                    Title = p.Title,

                    Content = p.Content,

                    DateCreated = p.DateCreated,

                    Type = p.Type,

                    Images = p.Images,

                    ImagePaths = p.Images?
                        .Select(i => i.ImagePath)
                        .ToList()
                        ?? new List<string>(),

                    Comments = p.Comments
                        ?? new List<PetFeedComment>(),

                    CommentCount =
                        p.Comments?.Count ?? 0,

                    PawCount =
                        p.Paws?.Count ?? 0,

                    IsPawed =
                        p.Paws != null &&
                        p.Paws.Any(
                            x => x.MemberId == userId),

                    IsHighlighted =
                        postId == p.PetFeedId
                })
                .ToList();

            // Do not expose Marketplace/Lost & Found when City is missing.
            return View(petFeedOnlyModel);
        }

        // ----------------------------------------------------------
        // GET COMBINED MEMBER FEED
        // ----------------------------------------------------------

        // Create a new random seed when this is a fresh PetFeed load.
        // When returning through normal navigation, petfeed.js supplies the saved
        // seed so the exact same feed ordering can be reconstructed.
        long currentFeedSeed = feedSeed ?? CreateFeedSeed();

        // Store the seed so Feed.cshtml can give it to petfeed.js.
        // JavaScript will reuse this seed when requesting additional pages.
        ViewData["FeedSeed"] = currentFeedSeed;

        // Get the first combined batch using the current feed seed.
        // The same seed must be supplied to every later LoadMore request.
        var result = await GetFeedPageAsync(
            user.City,
            userId,
            1,
            currentFeedSeed);

        // Apply the existing postId highlighting behavior to PetFeed items.
        //
        // Marketplace and Lost & Found do not use PetFeed highlighting.
        foreach (var item in result.Items)
        {
            if (item.ContentType ==
                PetFeedContentType.PetFeed)
            {
                // Only PetFeed items can be highlighted.
                item.IsHighlighted =
                    item.ContentId == postId;
            }
        }


        // Store HasMore for the Razor view.
        //
        // This will be consumed later when the infinite-scroll UI
        // is implemented.
        ViewData["HasMore"] = result.HasMore;


        // Return the existing Feed.cshtml.
        //
        // The ViewModel is now capable of containing all three content types,
        // but the Razor view will be updated in the next step to render them.
        return View(result.Items);
    }

    // Displays one administrator announcement to Members and anonymous visitors.
    //
    // This is intentionally separate from Details() because Details() is the
    // administrator-only PetFeed management details page.
    //
    // The member-facing page does NOT expose the administrator who created
    // the announcement.
    [AllowAnonymous]
    public async Task<IActionResult> AnnouncementDetails(int? id)
    {
        // Return 404 when no announcement ID was supplied.
        if (id == null)
        {
            return NotFound();
        }

        // Retrieve only the selected PetFeed announcement.
        // Include(p => p.Images) loads the related images so the details page
        // can display the announcement's first uploaded image.
        var announcement = await _context.PetFeeds
            .AsNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p =>
                p.PetFeedId == id &&
                p.Type == PetFeedType.Announcement);

        // Return 404 when the selected record does not exist
        // or is not an Announcement.
        if (announcement == null)
        {
            return NotFound();
        }

        // Return the announcement to the member-facing Razor view.
        //
        // The view will intentionally display only the announcement's public
        // information and will not display Admin or AdminId.
        return View(announcement);
    }

    // Adds a paw to an existing PetFeed.
    [HttpPost]
    [Authorize(Roles = "Member")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Paw(
        int id,
        long? feedSeed)
    {
        // Get the current member's Identity ID.
        var userId = _userManager.GetUserId(User);

        // Check whether this member has already pawed this PetFeed.
        var alreadyPawed = await _context.PetFeedPaws
            .AnyAsync(p =>
                p.PetFeedId == id &&
                p.MemberId == userId);

        // Only create a new paw when one does not already exist.
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

        // AJAX requests get a small JSON response instead of a redirect,
        // so petfeed.js can update the paw button/count in place without
        // reloading the page (and without losing scroll position).
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var currentPawCount = await _context.PetFeedPaws
                .CountAsync(p => p.PetFeedId == id);

            return Json(new
            {
                success = true,
                petFeedId = id,
                isPawed = true,
                pawCount = currentPawCount
            });
        }

        // Non-AJAX fallback (e.g. JavaScript disabled): forward the
        // current feedSeed so the redirect back to Feed keeps the same
        // random order instead of generating a new one.
        return RedirectToAction(
            nameof(Feed),
            new { feedSeed });
    }


    // Removes the current member's paw from a PetFeed.
    [HttpPost]
    [Authorize(Roles = "Member")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpaw(
        int id,
        long? feedSeed)
    {
        // Get the current member's Identity ID.
        var userId = _userManager.GetUserId(User);

        // Find this member's paw on the selected PetFeed.
        var paw = await _context.PetFeedPaws
            .FirstOrDefaultAsync(p =>
                p.PetFeedId == id &&
                p.MemberId == userId);

        // Remove the paw when it exists.
        if (paw != null)
        {
            _context.PetFeedPaws.Remove(paw);

            await _context.SaveChangesAsync();
        }

        // AJAX requests get a small JSON response instead of a redirect,
        // so petfeed.js can update the paw button/count in place without
        // reloading the page (and without losing scroll position).
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var currentPawCount = await _context.PetFeedPaws
                .CountAsync(p => p.PetFeedId == id);

            return Json(new
            {
                success = true,
                petFeedId = id,
                isPawed = false,
                pawCount = currentPawCount
            });
        }

        // Non-AJAX fallback: forward the current feedSeed so the redirect
        // back to Feed keeps the same random order instead of generating
        // a new one.
        return RedirectToAction(
            nameof(Feed),
            new { feedSeed });
    }


    // Adds a comment to an existing PetFeed.
    [HttpPost]
    [Authorize(Roles = "Member")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        int id,
        string content,
        long? feedSeed)
    {
        bool isAjax =
            Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // Reject empty comments.
        if (string.IsNullOrWhiteSpace(content))
        {
            if (isAjax)
            {
                return Json(new { success = false });
            }

            return RedirectToAction(
                nameof(Feed),
                new { feedSeed });
        }

        // Get the current member's Identity ID.
        var userId = _userManager.GetUserId(User);

        // Create the PetFeed comment.
        var comment = new PetFeedComment
        {
            PetFeedId = id,
            MemberId = userId,
            Content = content,
            DatePosted = DateTime.Now
        };

        // Add the comment to EF Core.
        _context.PetFeedComments.Add(comment);

        // Save the comment.
        await _context.SaveChangesAsync();

        // AJAX requests get the new comment's data back as JSON so
        // petfeed.js can append it to the comment list in place, instead
        // of redirecting and reloading the whole page.
        if (isAjax)
        {
            var member = await _userManager.FindByIdAsync(userId);

            var commentCount = await _context.PetFeedComments
                .CountAsync(c => c.PetFeedId == id);

            return Json(new
            {
                success = true,
                petFeedId = id,
                commentId = comment.CommentId,
                content = comment.Content,
                datePosted = comment.DatePosted.ToString("MMM dd, yyyy"),
                firstName = member?.FirstName,
                lastName = member?.LastName,
                profilePicturePath = member?.ProfilePicturePath,
                canDelete = true,
                commentCount
            });
        }

        // Forward the current feedSeed so the redirect back to Feed keeps
        // the same random order instead of generating a new one.
        return RedirectToAction(
            nameof(Feed),
            new { feedSeed });
    }


    // Deletes a PetFeed comment.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> DeleteComment(
        int id,
        long? feedSeed)
    {
        // Get the current user's Identity ID.
        var userId = _userManager.GetUserId(User);

        // Find the selected comment.
        var comment = await _context.PetFeedComments
            .FirstOrDefaultAsync(c => c.CommentId == id);

        // Return 404 when the comment does not exist.
        if (comment == null)
        {
            return NotFound();
        }

        // Members can only delete their own comments.
        // Administrators can delete comments regardless of ownership.
        if (!User.IsInRole("Admin") &&
            comment.MemberId != userId)
        {
            return Forbid();
        }

        int petFeedId = comment.PetFeedId;

        // Remove the comment.
        _context.PetFeedComments.Remove(comment);

        // Save the deletion.
        await _context.SaveChangesAsync();

        // AJAX requests get a small JSON response instead of a redirect,
        // so petfeed.js can remove the comment from the DOM in place.
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var commentCount = await _context.PetFeedComments
                .CountAsync(c => c.PetFeedId == petFeedId);

            return Json(new
            {
                success = true,
                commentId = id,
                petFeedId,
                commentCount
            });
        }

        // Forward the current feedSeed so the redirect back to Feed keeps
        // the same random order instead of generating a new one.
        return RedirectToAction(
            nameof(Feed),
            new { feedSeed });
    }



    //==========================================================
    //               COMBINED FEED BACKEND
    //==========================================================


    // Represents one lightweight candidate in the combined feed.
    // Only scalar fields are loaded at this stage.
    // Images, comments, paws, and other relationships are loaded only
    // after pagination identifies the 10 records that will be displayed.
    private sealed class FeedCandidate
    {
        // Identifies the source of this candidate:
        // PetFeed, Marketplace, or Lost & Found.
        public PetFeedContentType ContentType { get; set; }

        // Stores the primary key of the source record.
        // This can be PetFeedId, ListingId, or LostFoundId.
        public int ContentId { get; set; }

        // Common feed information.
        public string Title { get; set; }

        public string? Content { get; set; }

        public DateTime DateCreated { get; set; }

        // PetFeed-specific type.
        public PetFeedType? PetFeedType { get; set; }

        // Location fields used by Marketplace and Lost & Found.
        public string? City { get; set; }

        public string? Province { get; set; }

        // Marketplace-specific fields.
        public ListType? ListingType { get; set; }

        public ListPetType? ListingPetType { get; set; }

        public int? Price { get; set; }

        // Lost & Found-specific fields.
        public LostFoundType? LostFoundType { get; set; }

        public PetType? LostFoundPetType { get; set; }

        // Stable randomized position for today's feed.
        public long RandomKey { get; set; }
    }


    // Creates a new random seed for a fresh PetFeed session.
    // Unlike the previous daily seed, this value changes whenever a fresh
    // Feed request is created, including after a browser refresh.
    private static long CreateFeedSeed()
    {
        // Random.Shared is the built-in .NET random number generator.
        // It provides a new pseudo-random integer each time this method runs.
        long seed = Random.Shared.NextInt64(
            1,
            2147483628L);

        // Return the generated seed.
        // This seed will be reused by every pagination request for this feed.
        return seed;
    }

    // Builds the three independent feed queries using the supplied feed seed.
    // The seed is passed from Feed() or LoadMore() so every page uses the
    // exact same randomized ordering.
    private (
        IQueryable<FeedCandidate> PetFeeds,
        IQueryable<FeedCandidate> Marketplace,
        IQueryable<FeedCandidate> LostFound)
        BuildFeedCandidateQueries(
            string city,
            string? userId,
            long seed)
    {

        // Prime number used to keep the generated random key in a safe range.
        const long prime = 2147483629L;


        // ==========================================================
        // PETFEED
        // ==========================================================

        // Only Pet Tips remain in the normal combined feed.
        // Announcements are handled separately by the announcement carousel
        // and therefore must not enter pagination.
        var petFeedQuery = _context.PetFeeds
            .AsNoTracking()
            .Where(p => p.Type == PetFeedType.PetTip)
            .AsQueryable();

        // Convert PetFeed records into lightweight candidates.
        var petFeeds = petFeedQuery.Select(p => new FeedCandidate
        {
            // This candidate came from administrator-created PetFeed content.
            ContentType = PetFeedContentType.PetFeed,

            // PetFeed uses PetFeedId as its source ID.
            ContentId = p.PetFeedId,

            Title = p.Title,

            Content = p.Content,

            DateCreated = p.DateCreated,

            // PetFeed-specific type.
            PetFeedType = p.Type,

            // PetFeed does not have location fields.
            City = null,
            Province = null,

            // These properties are not used by PetFeed.
            ListingType = null,
            ListingPetType = null,
            Price = null,

            LostFoundType = null,
            LostFoundPetType = null,

            // Generate a deterministic daily ordering value.
            RandomKey =
                (((long)p.PetFeedId * seed) +
                1000000007L) % prime
        });


        // ==========================================================
        // MARKETPLACE
        // ==========================================================

        // Marketplace eligibility:
        //
        // Status       = Approved
        // ListStatus   = Pending
        // City         = current user's City
        // MemberId     != current user's ID
        var marketplace = _context.Listings
            .AsNoTracking()
            .Where(l =>
                l.Status == ListApprovalStatus.Approved &&
                l.ListStatus == ListingStatus.Pending &&
                l.City == city &&
                l.MemberId != userId)
            .Select(l => new FeedCandidate
            {
                // This candidate came from Marketplace.
                ContentType = PetFeedContentType.Marketplace,

                // Listing uses ListingId.
                ContentId = l.ListingId,

                Title = l.Title,

                Content = l.Description,

                DateCreated = l.DatePosted,

                // Marketplace location.
                City = l.City,
                Province = l.Province,

                // Marketplace-specific values.
                ListingType = l.Type,
                ListingPetType = l.PetType,
                Price = l.Price,

                // These properties are not used by Marketplace.
                PetFeedType = null,

                LostFoundType = null,
                LostFoundPetType = null,

                // Generate the Marketplace candidate's daily ordering value.
                RandomKey =
                    (((long)l.ListingId * seed) +
                    2000000011L) % prime
            });


        // ==========================================================
        // LOST & FOUND
        // ==========================================================

        // Lost & Found eligibility:
        //
        // Status       = Approved
        // RStatus      = Active
        // City         = current user's City
        // UserId       != current user's ID
        var lostFound = _context.LostFounds
            .AsNoTracking()
            .Where(l =>
                l.Status == ApprovalStatus.Approved &&
                l.RStatus == ReportStatus.Active &&
                l.City == city &&
                l.UserId != userId)
            .Select(l => new FeedCandidate
            {
                // This candidate came from Lost & Found.
                ContentType = PetFeedContentType.LostFound,

                // Lost & Found uses LostFoundId.
                ContentId = l.LostFoundId,

                Title = l.Title,

                Content = l.Description,

                DateCreated = l.DateReported,

                // Lost & Found location.
                City = l.City,
                Province = l.Province,

                // These properties are not used by Lost & Found.
                PetFeedType = null,

                ListingType = null,
                ListingPetType = null,
                Price = null,

                // Lost & Found-specific values.
                LostFoundType = l.Type,
                LostFoundPetType = l.PetType,

                // Generate the Lost & Found candidate's daily ordering value.
                RandomKey =
                    (((long)l.LostFoundId * seed) +
                    3000000017L) % prime
            });


        // Return the three independent queries.
        //
        // Nothing is executed here. ToListAsync() is performed later
        // by GetFeedPageAsync().
        return (
            petFeeds,
            marketplace,
            lostFound
        );
    }

    // Retrieves one page of the combined feed using a supplied feed seed.
    // The seed is identical for every page belonging to the same feed session,
    // which prevents pagination from reshuffling previously unseen posts.
    private async Task<(
        List<PetFeedFeedViewModel> Items,
        bool HasMore)> GetFeedPageAsync(
        string city,
        string? userId,
        int page,
        long feedSeed)
    {
        // Each feed page displays at most 10 posts.
        const int pageSize = 10;

        // Prevent invalid page numbers such as 0 or negative values.
        page = Math.Max(page, 1);

        // Request enough candidates from every source to build the requested
        // page and determine whether another page exists.
        //
        // Example:
        // Page 1 -> 11 candidates from each source
        // Page 2 -> 21 candidates from each source
        //
        // The extra record allows us to determine HasMore.
        int candidateLimit = (page * pageSize) + 1;


        // ==========================================================
        // BUILD THE THREE INDEPENDENT CANDIDATE QUERIES
        // ==========================================================

        // Build all three source queries using the exact same seed.
        // This guarantees that Page 1, Page 2, Page 3, etc. use one consistent
        // randomized ordering.
        var queries = BuildFeedCandidateQueries(
            city,
            userId,
            feedSeed);

        // ==========================================================
        // EXECUTE PETFEED QUERY
        // ==========================================================

        // Retrieve enough PetFeed candidates for the requested page.
        // The database performs the ordering before returning the records.
        var petFeedCandidates = await queries.PetFeeds
            .OrderBy(x => x.RandomKey)
            .ThenBy(x => x.ContentId)
            .Take(candidateLimit)
            .ToListAsync();


        // ==========================================================
        // EXECUTE MARKETPLACE QUERY
        // ==========================================================

        // Retrieve enough eligible Marketplace candidates.
        // City, approval, lifecycle, and ownership filters were already
        // applied inside BuildFeedCandidateQueries().
        var marketplaceCandidates = await queries.Marketplace
            .OrderBy(x => x.RandomKey)
            .ThenBy(x => x.ContentId)
            .Take(candidateLimit)
            .ToListAsync();


        // ==========================================================
        // EXECUTE LOST & FOUND QUERY
        // ==========================================================

        // Retrieve enough eligible Lost & Found candidates.
        var lostFoundCandidates = await queries.LostFound
            .OrderBy(x => x.RandomKey)
            .ThenBy(x => x.ContentId)
            .Take(candidateLimit)
            .ToListAsync();


        // ==========================================================
        // COMBINE ALL SOURCES
        // ==========================================================

        // Combine the three independently retrieved candidate lists.
        //
        // Concat() is now running on normal in-memory List<T> objects,
        // not IQueryable objects, so EF Core does not have to translate
        // this operation into SQL.
        var candidates = petFeedCandidates
            .Concat(marketplaceCandidates)
            .Concat(lostFoundCandidates)

            // Apply the existing daily deterministic random ordering.
            // This creates the TRUE RANDOM mixed feed without forcing
            // a specific number of posts from each content type.
            .OrderBy(x => x.RandomKey)

            // If two records receive the same RandomKey, ContentType gives
            // the ordering a stable secondary value.
            .ThenBy(x => x.ContentType)

            // ContentId provides one final deterministic tie-breaker.
            .ThenBy(x => x.ContentId)

            .ToList();


        // ==========================================================
        // DETERMINE WHETHER ANOTHER PAGE EXISTS
        // ==========================================================

        // Because each source supplied enough candidates for the requested
        // page, the combined list can be checked for another page.
        bool hasMore =
            candidates.Count >
            page * pageSize;


        // ==========================================================
        // SELECT CURRENT PAGE
        // ==========================================================

        // Select only the 10 candidates belonging to the requested page.
        //
        // This is the point where the true random ordering becomes the
        // actual visible feed order.
        var selectedCandidates = candidates
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();


        // ==========================================================
        // GET SOURCE IDS FOR SELECTED POSTS
        // ==========================================================

        // Extract only the PetFeed IDs that were selected for this page.
        var petFeedIds = selectedCandidates
            .Where(x =>
                x.ContentType ==
                PetFeedContentType.PetFeed)
            .Select(x => x.ContentId)
            .ToList();

        // Extract only the Marketplace Listing IDs selected for this page.
        var listingIds = selectedCandidates
            .Where(x =>
                x.ContentType ==
                PetFeedContentType.Marketplace)
            .Select(x => x.ContentId)
            .ToList();

        // Extract only the Lost & Found IDs selected for this page.
        var lostFoundIds = selectedCandidates
            .Where(x =>
                x.ContentType ==
                PetFeedContentType.LostFound)
            .Select(x => x.ContentId)
            .ToList();

        // Get the Member IDs of the Marketplace listings selected for this page.
        // These IDs will be used to retrieve the actual post owners.
        var marketplaceOwnerIds = await _context.Listings
            .AsNoTracking()
            .Where(l => listingIds.Contains(l.ListingId))
            .Select(l => l.MemberId)
            .Where(id => id != null)
            .Distinct()
            .ToListAsync();

        // Get the user IDs of the Lost & Found reports selected for this page.
        // These IDs will be used to retrieve the actual report owners.
        var lostFoundOwnerIds = await _context.LostFounds
            .AsNoTracking()
            .Where(l => lostFoundIds.Contains(l.LostFoundId))
            .Select(l => l.UserId)
            .Where(id => id != null)
            .Distinct()
            .ToListAsync();

        // Combine the owner IDs from Marketplace and Lost & Found.
        // Distinct() prevents retrieving the same ApplicationUser more than once
        // when the same member has both types of posts in the feed.
        var ownerIds = marketplaceOwnerIds
            .Concat(lostFoundOwnerIds)
            .Distinct()
            .ToList();

        // Retrieve the actual ApplicationUser records for the selected owners.
        // Only selected-page owners are loaded, rather than every user in the database.
        var owners = await _userManager.Users
            .AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        // ==========================================================
        // LOAD SELECTED PETFEEDS
        // ==========================================================

        // Load only PetFeed records that actually appear on this page.
        //
        // Images, Comments, and Paws are included because these are existing
        // PetFeed features that must continue working.
        var petFeeds = await _context.PetFeeds
            .AsNoTracking()
            .Where(p =>
                petFeedIds.Contains(p.PetFeedId))
            .Include(p => p.Images)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Member)
            .Include(p => p.Paws)
            .ToListAsync();


        // ==========================================================
        // LOAD SELECTED MARKETPLACE LISTINGS
        // ==========================================================

        // Load only the Marketplace listings selected for the current page.
        // Their images are included because they will be displayed by the
        // Marketplace card later.
        var listings = await _context.Listings
            .AsNoTracking()
            .Where(l =>
                listingIds.Contains(l.ListingId))
            .Include(l => l.Images)
            .ToListAsync();


        // ==========================================================
        // LOAD SELECTED LOST & FOUND REPORTS
        // ==========================================================

        // Load only the Lost & Found reports selected for the current page.
        var lostFounds = await _context.LostFounds
            .AsNoTracking()
            .Where(l =>
                lostFoundIds.Contains(l.LostFoundId))
            .Include(l => l.Images)
            .ToListAsync();


        // ==========================================================
        // BUILD FINAL VIEWMODEL LIST
        // ==========================================================

        // This list will be passed to Feed.cshtml.
        var model =
            new List<PetFeedFeedViewModel>();


        // Process ONLY the selected 10 candidates.
        //
        // The previous implementation processed the entire candidate list.
        // That could cause records outside the current page to be loaded into
        // the ViewModel, which is incorrect for pagination.
        foreach (var candidate in selectedCandidates)
        {
            // ======================================================
            // PETFEED
            // ======================================================

            if (candidate.ContentType ==
                PetFeedContentType.PetFeed)
            {
                // Find the actual PetFeed entity using its source ID.
                var post = petFeeds.FirstOrDefault(
                    p => p.PetFeedId ==
                         candidate.ContentId);

                // If the record was deleted between the candidate query
                // and the loading query, simply skip it.
                if (post == null)
                {
                    continue;
                }

                model.Add(new PetFeedFeedViewModel
                {
                    // Identify this item as PetFeed.
                    ContentType =
                        PetFeedContentType.PetFeed,

                    // Preserve the original PetFeed ID.
                    PetFeedId =
                        post.PetFeedId,

                    // ContentId is the common ID used by the combined feed.
                    ContentId =
                        post.PetFeedId,

                    Title =
                        post.Title,

                    Content =
                        post.Content,

                    DateCreated =
                        post.DateCreated,

                    Type =
                        post.Type,

                    // Preserve the existing PetFeed image collection.
                    Images =
                        post.Images,

                    // Populate the common image path list.
                    ImagePaths =
                        post.Images?
                            .Select(i =>
                                i.ImagePath)
                            .ToList()
                        ?? new List<string>(),

                    // Preserve existing PetFeed comments.
                    Comments =
                        post.Comments ??
                        new List<PetFeedComment>(),

                    CommentCount =
                        post.Comments?.Count ??
                        0,

                    // Preserve existing PetFeed Paw functionality.
                    PawCount =
                        post.Paws?.Count ??
                        0,

                    IsPawed =
                        userId != null &&
                        post.Paws != null &&
                        post.Paws.Any(
                            p => p.MemberId ==
                                 userId),

                    // Feed() sets the actual highlighted state afterward.
                    IsHighlighted =
                        false,

                    DetailsUrl =
                        string.Empty
                });

                // Continue to the next selected candidate.
                continue;
            }


            // ======================================================
            // MARKETPLACE
            // ======================================================

            if (candidate.ContentType ==
                PetFeedContentType.Marketplace)
            {
                // Find the actual Listing using the common ContentId.
                var listing =
                    listings.FirstOrDefault(
                        l => l.ListingId ==
                             candidate.ContentId);

                // Skip the candidate if the listing no longer exists.
                if (listing == null)
                {
                    continue;
                }

                // Find the ApplicationUser who created this Marketplace listing.
                // This ensures the feed displays the actual member rather than Admin.
                var listingOwner = owners.TryGetValue(
                    listing.MemberId ?? string.Empty,
                    out var marketplaceOwner)
                        ? marketplaceOwner
                        : null;

                model.Add(new PetFeedFeedViewModel
                {
                    // Identify this item as Marketplace.
                    ContentType =
                        PetFeedContentType.Marketplace,

                    ContentId =
                        listing.ListingId,

                    Title =
                        listing.Title,

                    Content =
                        listing.Description,

                    DateCreated =
                        listing.DatePosted,

                    // Store the actual Marketplace owner's information.
                    OwnerId =
                        listing.MemberId,

                    // Build the actual member's display name from their first and last name.
                    // Trim() removes extra spaces if either name is missing.
                    OwnerName = listingOwner == null
                    ? null
                    : $"{listingOwner.FirstName} {listingOwner.LastName}".Trim(),

                    OwnerProfileImage =
                        listingOwner?.ProfilePicturePath,

                    City =
                        listing.City,

                    Province =
                        listing.Province,

                    ListingType =
                        listing.Type,

                    ListingPetType =
                        listing.PetType,

                    Price =
                        listing.Price,

                    ImagePaths =
                        listing.Images?
                            .Select(i => i.ImagePath)
                            .ToList()
                        ?? new List<string>(),

                    PawCount = 0,

                    CommentCount = 0,

                    IsPawed = false,

                    IsHighlighted = false,

                    Comments =
                        new List<PetFeedComment>(),

                    DetailsUrl =
                     $"/Listings/MarketplaceDetails/{listing.ListingId}"
                });

                // Continue to the next selected candidate.
                continue;
            }


            // ======================================================
            // LOST & FOUND
            // ======================================================

            if (candidate.ContentType ==
                PetFeedContentType.LostFound)
            {
                // Find the actual Lost & Found record using ContentId.
                var report =
                    lostFounds.FirstOrDefault(
                        l => l.LostFoundId ==
                             candidate.ContentId);

                // Skip the candidate if the report no longer exists.
                if (report == null)
                {
                    continue;
                }

                // Find the ApplicationUser who created this Lost & Found report.
                // This ensures the feed shows the actual member who posted it.
                var reportOwner = owners.TryGetValue(
                    report.UserId ?? string.Empty,
                    out var lostFoundOwner)
                        ? lostFoundOwner
                        : null;

                model.Add(new PetFeedFeedViewModel
                {
                    // Identify this item as Lost & Found.
                    ContentType =
                        PetFeedContentType.LostFound,

                    ContentId =
                        report.LostFoundId,

                    Title =
                        report.Title,

                    Content =
                        report.Description,

                    DateCreated =
                        report.DateReported,

                    // Store the actual Lost & Found owner's information.
                    OwnerId =
                        report.UserId,

                    // Build the actual member's display name from their first and last name.
                    // Trim() removes extra spaces if either name is missing.
                    OwnerName = reportOwner == null
                    ? null
                    : $"{reportOwner.FirstName} {reportOwner.LastName}".Trim(),

                    OwnerProfileImage =
                        reportOwner?.ProfilePicturePath,

                    City =
                        report.City,

                    Province =
                        report.Province,

                    LostFoundType =
                        report.Type,

                    LostFoundPetType =
                        report.PetType,

                    ImagePaths =
                        report.Images?
                            .Select(i => i.ImagePath)
                            .ToList()
                        ?? new List<string>(),

                    PawCount = 0,

                    CommentCount = 0,

                    IsPawed = false,

                    IsHighlighted = false,

                    Comments =
                        new List<PetFeedComment>(),

                    DetailsUrl =
                     $"/LostFounds/BrowseDetails/{report.LostFoundId}"
                });
            }
        }


        // Return the final current page together with the pagination state.
        return (model, hasMore);
    }

    //==========================================================
    //                     AJAX LOAD MORE
    //==========================================================


    // AJAX endpoint used by petfeed.js to request the next feed batch.
    // The feedSeed must be supplied so pagination continues using the same
    // randomized ordering as the initial PetFeed request.
    [HttpGet]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> LoadMore(
        int page = 1,
        long feedSeed = 0)
    {
        // Retrieve the currently authenticated member.
        var user = await _userManager
            .GetUserAsync(User);

        // Return 401 if the Identity user cannot be found.
        if (user == null)
        {
            return Unauthorized();
        }

        // Marketplace and Lost & Found require the user's City.
        // Without a City, the strict location filter cannot be applied.
        if (string.IsNullOrWhiteSpace(user.City))
        {
            return BadRequest(
                "Your account does not have a city set.");
        }

        // Reject a missing seed because pagination must use the same seed that
        // created the original PetFeed ordering.
        if (feedSeed <= 0)
        {
            return BadRequest(
                "A valid feed seed is required.");
        }

        // Retrieve the requested page using the same feed seed as Page 1.
        // This prevents the ordering from changing between pagination requests.
        var result = await GetFeedPageAsync(
            user.City,
            user.Id,
            page,
            feedSeed);

        // Pass the HasMore value to the partial view.
        // petfeed.js will use this to determine whether it should
        // request another batch.
        ViewData["HasMore"] = result.HasMore;

        // Return only the feed cards.
        // The main PetFeed page itself is not reloaded.
        return PartialView(
            "_FeedItems",
            result.Items);
    }

}