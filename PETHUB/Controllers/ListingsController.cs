using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;
using System.Security.Claims;

namespace PETHUB.Controllers
{
    public class ListingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly AuditLogService _auditLogService;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================
        //
        // Combines the services required by both branches:
        // - Database access
        // - ASP.NET Identity
        // - Notifications
        // - Audit logging
        // =========================================================

        public ListingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService,
            AuditLogService auditLogService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
        }


        // =========================================================
        // ADMIN - MARKETPLACE MANAGEMENT
        // =========================================================

        // GET: Listings
        //
        // Displays Marketplace listings for administrators.
        // Supports:
        // - Approval status
        // - Listing type
        // - Pet type
        // - Pagination
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(
            string? status,
            string? listingType,
            string? petType,
            int page = 1)
        {
            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }


            // Start with all Marketplace listings.
            var listings = _context.Listings
                .Include(l => l.Member)
                .Include(l => l.Images)
                .AsQueryable();


            // -----------------------------------------------------
            // APPROVAL STATUS FILTER
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ListApprovalStatus>(
                    status,
                    out var selectedStatus))
            {
                listings = listings.Where(
                    l => l.Status == selectedStatus);
            }


            // -----------------------------------------------------
            // LISTING TYPE FILTER
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(listingType) &&
                Enum.TryParse<ListType>(
                    listingType,
                    out var selectedListingType))
            {
                listings = listings.Where(
                    l => l.Type == selectedListingType);
            }


            // -----------------------------------------------------
            // PET TYPE FILTER
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(petType) &&
                Enum.TryParse<ListPetType>(
                    petType,
                    out var selectedPetType))
            {
                listings = listings.Where(
                    l => l.PetType == selectedPetType);
            }


            // -----------------------------------------------------
            // PAGINATION
            // -----------------------------------------------------

            var totalItems =
                await listings.CountAsync();

            var totalPages =
                (int)Math.Ceiling(
                    totalItems / (double)pageSize);

            if (totalPages > 0 &&
                page > totalPages)
            {
                page = totalPages;
            }


            var pagedListings =
                await listings
                    .OrderByDescending(
                        l => l.DatePosted)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();


            var result =
                new PaginationViewModel<Listing>
                {
                    Items = pagedListings,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                };


            return View(result);
        }


        // =========================================================
        // PUBLIC / MEMBER - MARKETPLACE
        // =========================================================

        // GET: Listings/Marketplace
        //
        // Guests can browse available Marketplace listings.
        //
        // Logged-in Members do not see their own listings here
        // because they manage those through My Posts.
        //
        // Listings owned by deactivated accounts are also hidden.
        [AllowAnonymous]
        public async Task<IActionResult> Marketplace(
     string? search,
     string? listingType,
     string? petType,
     int page = 1)
        {
            const int pageSize = 12;

            if (page < 1)
            {
                page = 1;
            }


            var memberId =
                _userManager.GetUserId(User);


            var listings =
                _context.Listings
                    .Include(l => l.Member)
                    .Include(l => l.Images)
                    .Where(l =>
                        l.Status ==
                            ListApprovalStatus.Approved
                        &&
                        l.ListStatus ==
                            ListingStatus.Pending
                        &&
                        l.MemberId != memberId
                        &&
                        l.Member != null
                        &&
                        l.Member.Status ==
                            UserStatus.Active)
                    .AsQueryable();


            // -----------------------------------------------------
            // SEARCH
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                listings = listings.Where(l =>
                    l.Title.Contains(search)
                    ||
                    (l.Description != null &&
                     l.Description.Contains(search))
                    ||
                    (l.City != null &&
                     l.City.Contains(search))
                    ||
                    (l.Province != null &&
                     l.Province.Contains(search))
                );
            }


            // -----------------------------------------------------
            // LISTING TYPE FILTER
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(listingType) &&
                Enum.TryParse<ListType>(
                    listingType,
                    out var selectedListingType))
            {
                listings =
                    listings.Where(
                        l => l.Type ==
                            selectedListingType);
            }


            // -----------------------------------------------------
            // PET TYPE FILTER
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(petType) &&
                Enum.TryParse<ListPetType>(
                    petType,
                    out var selectedPetType))
            {
                listings =
                    listings.Where(
                        l => l.PetType ==
                            selectedPetType);
            }


            // -----------------------------------------------------
            // PAGINATION
            // -----------------------------------------------------

            var totalItems =
                await listings.CountAsync();

            var totalPages =
                (int)Math.Ceiling(
                    totalItems / (double)pageSize);


            if (totalPages > 0 &&
                page > totalPages)
            {
                page = totalPages;
            }


            var pagedListings =
                await listings
                    .OrderByDescending(
                        l => l.DatePosted)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();


            var result =
                new PaginationViewModel<Listing>
                {
                    Items = pagedListings,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                };


            return View(result);
        }


        // =========================================================
        // ADMIN - LISTING DETAILS
        // =========================================================

        // GET: Listings/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var listing =
                await _context.Listings
                    .Include(l => l.Member)
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


            if (listing == null)
            {
                return NotFound();
            }


            return View(listing);
        }


        // =========================================================
        // PUBLIC / MEMBER - MARKETPLACE DETAILS
        // =========================================================

        // GET: Listings/MarketplaceDetails/5
        [AllowAnonymous]
        public async Task<IActionResult> MarketplaceDetails(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var listing =
                await _context.Listings
                    .Include(l => l.Member)
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


            if (listing == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // OWNER REDIRECT
            // -----------------------------------------------------
            //
            // Owners should manage their listing through My Posts
            // instead of viewing the public version.
            // -----------------------------------------------------

            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUserId =
                    _userManager.GetUserId(User);

                if (listing.MemberId ==
                    currentUserId)
                {
                    return RedirectToAction(
                        "MarketplaceDetails",
                        "MyPosts",
                        new
                        {
                            id = listing.ListingId
                        });
                }
            }


            // -----------------------------------------------------
            // PUBLIC AVAILABILITY
            // -----------------------------------------------------

            if (
                listing.Status !=
                    ListApprovalStatus.Approved
                ||
                listing.ListStatus !=
                    ListingStatus.Pending
                ||
                listing.Member == null
                ||
                listing.Member.Status !=
                    UserStatus.Active)
            {
                return NotFound();
            }


            return View(listing);
        }


        // =========================================================
        // MEMBER - CREATE LISTING
        // =========================================================

        // GET: Listings/Create
        [HttpGet]
        [Authorize(Roles = "Member")]
        public IActionResult Create()
        {
            return View();
        }


        // POST: Listings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Create(
            Listing listing,
            List<IFormFile> ImageFiles)
        {
            if (!ModelState.IsValid)
            {
                return View(listing);
            }


            // -----------------------------------------------------
            // PREPARE AND SAVE LISTING
            // -----------------------------------------------------

            listing.DatePosted =
                DateTime.Now;

            listing.MemberId =
                _userManager.GetUserId(User);

            _context.Listings.Add(listing);

            await _context.SaveChangesAsync();


            string? imagePath = null;


            // -----------------------------------------------------
            // SAVE IMAGES
            // -----------------------------------------------------

            if (ImageFiles != null &&
                ImageFiles.Count > 0)
            {
                try
                {
                    var savedImages =
                        await ImageHelper.SaveImagesAsync(
                            ImageFiles,
                            listing.ListingId,
                            (id, path) =>
                                new ListingImage
                                {
                                    ListingId = id,
                                    ImagePath = path
                                },
                            "marketplace");


                    _context.ListingImages
                        .AddRange(savedImages);

                    await _context.SaveChangesAsync();


                    imagePath =
                        savedImages
                            .FirstOrDefault()
                            ?.ImagePath;
                }
                catch (Exception)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Some images could not be uploaded.");

                    return View(listing);
                }
            }


            // -----------------------------------------------------
            // AUDIT LOG
            // -----------------------------------------------------
            //
            // Log only after the listing and its images have
            // successfully completed the normal save process.
            // -----------------------------------------------------

            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Created Post");
            }


            // -----------------------------------------------------
            // NOTIFY ADMINISTRATORS
            // -----------------------------------------------------

            var admins =
                await _userManager
                    .GetUsersInRoleAsync("Admin");


            string notificationTitle;
            string notificationMessage;


            if (listing.Type ==
                ListType.For_Adoption)
            {
                notificationTitle =
                    "Adoption Request";

                notificationMessage =
                    "A new Adoption Request is waiting for Approval.";
            }
            else
            {
                notificationTitle =
                    "Marketplace Listing Request";

                notificationMessage =
                    "A new Marketplace Listing Request is waiting for Approval.";
            }


            foreach (var admin in admins)
            {
                await _notificationService
                    .CreateNotificationAsync(
                        admin.Id,
                        NotificationType
                            .NewMarketplaceSubmission,
                        notificationTitle,
                        notificationMessage,
                        imagePath,
                        "/Listings/Details/" +
                            listing.ListingId,
                        listingId:
                            listing.ListingId);
            }


            TempData["SuccessMessage"] =
                "Your Marketplace listing has been submitted for approval.";


            return RedirectToAction(
                nameof(Marketplace));
        }


        // =========================================================
        // MEMBER - EDIT LISTING
        // =========================================================

        // GET: Listings/Edit/5
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Edit(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var listing =
                await _context.Listings
                    .Include(l => l.Images)
                    .Include(l => l.Member)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


            if (listing == null)
            {
                return NotFound();
            }


            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            // Members may edit only their own listings.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }


            // Approved listings cannot be edited.
            if (listing.Status ==
                ListApprovalStatus.Approved)
            {
                return Forbid();
            }


            return View(listing);
        }


        // POST: Listings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Edit(
            int id,
            Listing listing,
            List<IFormFile> ImageFiles,
            List<int> DeletedImageIds)
        {
            if (id != listing.ListingId)
            {
                return NotFound();
            }


            var existingListing =
                await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


            if (existingListing == null)
            {
                return NotFound();
            }


            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            // Members may edit only their own listing.
            if (existingListing.MemberId !=
                userId)
            {
                return Forbid();
            }


            // Approved listings cannot be edited.
            if (existingListing.Status ==
                ListApprovalStatus.Approved)
            {
                return Forbid();
            }


            // Remember whether this post was removed.
            // Editing a Removed listing resubmits it.
            var wasRemoved =
                existingListing.Status ==
                    ListApprovalStatus.Removed;


            // -----------------------------------------------------
            // FORM VALIDATION
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                listing.Images =
                    existingListing.Images;

                listing.MemberId =
                    existingListing.MemberId;

                listing.Status =
                    existingListing.Status;

                return View(listing);
            }


            // -----------------------------------------------------
            // UPDATE LISTING INFORMATION
            // -----------------------------------------------------

            existingListing.Title =
                listing.Title;

            existingListing.Description =
                listing.Description;

            existingListing.Price =
                listing.Price;

            existingListing.Province =
                listing.Province;

            existingListing.City =
                listing.City;

            existingListing.Barangay =
                listing.Barangay;

            existingListing.StreetAddress =
                listing.StreetAddress;

            existingListing.Breed =
                listing.Breed;

            existingListing.PetType =
                listing.PetType;

            existingListing.PetSex =
                listing.PetSex;

            existingListing.Type =
                listing.Type;

            existingListing.DatePosted =
                DateTime.Now;


            // -----------------------------------------------------
            // REMOVED → PENDING
            // -----------------------------------------------------

            if (wasRemoved)
            {
                existingListing.Status =
                    ListApprovalStatus.Pending;
            }


            // -----------------------------------------------------
            // DELETE SELECTED EXISTING IMAGES
            // -----------------------------------------------------

            if (DeletedImageIds != null &&
                DeletedImageIds.Any())
            {
                foreach (var imageId in
                    DeletedImageIds)
                {
                    var image =
                        existingListing.Images
                            .FirstOrDefault(i =>
                                i.ListingImageId ==
                                imageId);


                    if (image == null)
                    {
                        continue;
                    }


                    var filePath =
                        Path.Combine(
                            Directory
                                .GetCurrentDirectory(),
                            "wwwroot",
                            image.ImagePath
                                .TrimStart('/'));


                    if (System.IO.File.Exists(
                        filePath))
                    {
                        System.IO.File.Delete(
                            filePath);
                    }


                    _context.ListingImages
                        .Remove(image);
                }
            }


            // -----------------------------------------------------
            // ADD NEW IMAGES
            // -----------------------------------------------------

            if (ImageFiles != null &&
                ImageFiles.Count > 0)
            {
                var savedImages =
                    await ImageHelper.SaveImagesAsync(
                        ImageFiles,
                        existingListing.ListingId,
                        (listingId, path) =>
                            new ListingImage
                            {
                                ListingId =
                                    listingId,

                                ImagePath =
                                    path
                            },
                        "marketplace");


                _context.ListingImages
                    .AddRange(savedImages);
            }


            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // AUDIT LOG
            // -----------------------------------------------------

            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Edited Post");
            }


            // -----------------------------------------------------
            // REMOVED LISTING RESUBMISSION NOTIFICATION
            // -----------------------------------------------------

            if (wasRemoved)
            {
                var admins =
                    await _userManager
                        .GetUsersInRoleAsync(
                            "Admin");


                string notificationTitle;
                string notificationMessage;


                if (existingListing.Type ==
                    ListType.For_Adoption)
                {
                    notificationTitle =
                        "Adoption Listing Resubmitted";

                    notificationMessage =
                        "A previously removed adoption listing has been edited and resubmitted for approval.";
                }
                else
                {
                    notificationTitle =
                        "Marketplace Listing Resubmitted";

                    notificationMessage =
                        "A previously removed Marketplace listing has been edited and resubmitted for approval.";
                }


                foreach (var admin in admins)
                {
                    await _notificationService
                        .CreateNotificationAsync(
                            admin.Id,
                            NotificationType
                                .NewMarketplaceSubmission,
                            notificationTitle,
                            notificationMessage,
                            existingListing.Images
                                .FirstOrDefault()
                                ?.ImagePath,
                            "/Listings/Details/" +
                                existingListing.ListingId,
                            listingId:
                                existingListing.ListingId);
                }


                TempData["SuccessMessage"] =
                    "Your listing has been updated and resubmitted for approval.";
            }
            else
            {
                TempData["SuccessMessage"] =
                    "Marketplace listing updated successfully.";
            }


            return RedirectToAction(
                "MarketplaceDetails",
                "MyPosts",
                new
                {
                    id =
                        existingListing.ListingId
                });
        }


        // =========================================================
        // MEMBER - REMOVE SINGLE IMAGE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> RemoveImage(
            int id)
        {
            var image =
                await _context.ListingImages
                    .Include(i => i.Listing)
                    .FirstOrDefaultAsync(
                        i => i.ListingImageId ==
                            id);


            if (image == null)
            {
                return NotFound();
            }


            var userId =
                _userManager.GetUserId(User);


            // Only the listing owner may remove images.
            if (image.Listing.MemberId !=
                userId)
            {
                return Forbid();
            }


            var listingId =
                await ImageHelper
                    .RemoveImageAsync(
                        _context,
                        _context.ListingImages,
                        id,
                        img => img.ImagePath,
                        img => img.ListingId);


            return RedirectToAction(
                nameof(Edit),
                new
                {
                    id = listingId
                });
        }


        // =========================================================
        // ADMIN - APPROVE LISTING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(
            int id)
        {
            var listing =
                await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


            if (listing == null)
            {
                return NotFound();
            }


            // Prevent repeated approval from creating
            // duplicate notifications and audit entries.
            if (listing.Status ==
                ListApprovalStatus.Approved)
            {
                TempData["InfoMessage"] =
                    "This Marketplace listing is already approved.";

                return RedirectToAction(
                    nameof(Index));
            }


            listing.Status =
                ListApprovalStatus.Approved;

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // AUDIT LOG
            // -----------------------------------------------------

            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Approved Post");
            }


            // -----------------------------------------------------
            // MEMBER NOTIFICATION
            // -----------------------------------------------------

            string notificationTitle;
            string notificationMessage;


            if (listing.Type ==
                ListType.For_Adoption)
            {
                notificationTitle =
                    "Adoption Request Approved";

                notificationMessage =
                    "Your adoption listing is now visible in the Marketplace.";
            }
            else
            {
                notificationTitle =
                    "Marketplace Listing Approved";

                notificationMessage =
                    "Your listing is now visible in the Marketplace.";
            }


            await _notificationService
                .CreateNotificationAsync(
                    listing.MemberId,
                    NotificationType
                        .MarketplaceApproved,
                    notificationTitle,
                    notificationMessage,
                    listing.Images
                        .FirstOrDefault()
                        ?.ImagePath,
                    "/Listings/MarketplaceDetails/" +
                        listing.ListingId,
                    listingId:
                        listing.ListingId);


            TempData["SuccessMessage"] =
                "Marketplace listing approved successfully.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // ADMIN - REJECT LISTING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(
            int id)
        {
            var listing =
                await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id);


            if (listing == null)
            {
                return NotFound();
            }


            // Prevent repeated rejection from creating
            // duplicate notifications and audit entries.
            if (listing.Status ==
                ListApprovalStatus.Rejected)
            {
                TempData["InfoMessage"] =
                    "This Marketplace listing is already rejected.";

                return RedirectToAction(
                    nameof(Index));
            }


            listing.Status =
                ListApprovalStatus.Rejected;

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // AUDIT LOG
            // -----------------------------------------------------

            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Rejected Post");
            }


            // -----------------------------------------------------
            // MEMBER NOTIFICATION
            // -----------------------------------------------------

            string notificationTitle;
            string notificationMessage;


            if (listing.Type ==
                ListType.For_Adoption)
            {
                notificationTitle =
                    "Adoption Request Rejected";

                notificationMessage =
                    "Your adoption listing was rejected because it does not meet our community standards.";
            }
            else
            {
                notificationTitle =
                    "Marketplace Listing Request Rejected";

                notificationMessage =
                    "Your Marketplace listing was rejected because it does not meet our community standards.";
            }


            await _notificationService
                .CreateNotificationAsync(
                    listing.MemberId,
                    NotificationType
                        .MarketplaceRejected,
                    notificationTitle,
                    notificationMessage,
                    listing.Images
                        .FirstOrDefault()
                        ?.ImagePath,
                    "/Listings/MarketplaceDetails/" +
                        listing.ListingId,
                    listingId:
                        listing.ListingId);


            TempData["SuccessMessage"] =
                "Marketplace listing rejected successfully.";


            return RedirectToAction(
                nameof(Index));
        }
    }
}