using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.Services;
using System.Security.Claims;

namespace PETHUB.Controllers
{
    public class ListingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ListingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }


        // =========================================================
        // ADMIN - MARKETPLACE MANAGEMENT
        // =========================================================

        // GET: Listings
        //
        // Displays Marketplace listings for administrators.
        // Supports approval status, listing type, and pet type filters.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(
            string? status,
            string? listingType,
            string? petType)
        {
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
                listings =
                    listings.Where(
                        l => l.Status == selectedStatus
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
                        l => l.Type == selectedListingType
                    );
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
                        l => l.PetType == selectedPetType
                    );
            }


            return View(
                await listings.ToListAsync()
            );
        }


        // =========================================================
        // PUBLIC / MEMBER - MARKETPLACE
        // =========================================================

        // GET: Listings/Marketplace
        //
        // Public Marketplace page.
        // Guests can view all available approved listings.
        //
        // Logged-in Members do not see their own listings here
        // because they manage those through My Posts.
        [AllowAnonymous]
        public async Task<IActionResult> Marketplace(
            string? listingType,
            string? petType)
        {
            var memberId =
                _userManager.GetUserId(User);


            var listings =
                _context.Listings
                    .Include(l => l.Member)
                    .Include(l => l.Images)
                    .Where(l =>
                        l.Status == ListApprovalStatus.Approved &&
                        l.ListStatus == ListingStatus.Pending &&

                        // Do not show the logged-in Member's
                        // own listing in the public Marketplace.
                        l.MemberId != memberId &&

                        // Listings belonging to deactivated
                        // accounts should no longer be publicly available.
                        l.Member != null &&
                        l.Member.Status == UserStatus.Active
                    )
                    .AsQueryable();


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
                        l => l.Type == selectedListingType
                    );
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
                        l => l.PetType == selectedPetType
                    );
            }


            return View(
                await listings.ToListAsync()
            );
        }


        // =========================================================
        // ADMIN - LISTING DETAILS
        // =========================================================

        // GET: Listings/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
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
                        l => l.ListingId == id
                    );


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
        public async Task<IActionResult> MarketplaceDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // =====================================================
            // LOAD LISTING
            // =====================================================

            var listing =
                await _context.Listings
                    .Include(l => l.Member)
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(l =>
                        l.ListingId == id
                    );

            if (listing == null)
            {
                return NotFound();
            }


            // =====================================================
            // OWNER REDIRECT
            // =====================================================
            //
            // If the currently logged-in Member owns this listing,
            // send them to the owner-specific My Posts details page
            // instead of the public Marketplace details page.
            // =====================================================

            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUserId =
                    _userManager.GetUserId(User);

                if (listing.MemberId == currentUserId)
                {
                    return RedirectToAction(
                        "MarketplaceDetails",
                        "MyPosts",
                        new
                        {
                            id = listing.ListingId
                        }
                    );
                }
            }


            // =====================================================
            // PUBLIC AVAILABILITY CHECK
            // =====================================================

            if (
                listing.Status != ListApprovalStatus.Approved ||
                listing.ListStatus != ListingStatus.Pending ||
                listing.Member == null ||
                listing.Member.Status != UserStatus.Active
            )
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
            // PREPARE LISTING
            // -----------------------------------------------------

            listing.DatePosted = DateTime.Now;

            listing.MemberId =
                _userManager.GetUserId(User);


            // -----------------------------------------------------
            // SAVE LISTING
            // -----------------------------------------------------

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
                            "marketplace"
                        );


                    _context.ListingImages
                        .AddRange(savedImages);


                    await _context.SaveChangesAsync();


                    // First image is used by notifications.
                    imagePath =
                        savedImages
                            .FirstOrDefault()
                            ?.ImagePath;
                }
                catch (Exception)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Some images could not be uploaded."
                    );

                    return View(listing);
                }
            }


            // -----------------------------------------------------
            // NOTIFY ADMINISTRATORS
            // -----------------------------------------------------

            var admins =
                await _userManager
                    .GetUsersInRoleAsync("Admin");


            string notificationTitle;
            string notificationMessage;


            if (listing.Type == ListType.For_Adoption)
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
                        NotificationType.NewMarketplaceSubmission,
                        notificationTitle,
                        notificationMessage,
                        imagePath,
                        "/Listings/Details/" + listing.ListingId,
                        listingId: listing.ListingId
                    );
            }


            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            TempData["SuccessMessage"] =
                "Your Marketplace listing has been submitted for approval.";


            return RedirectToAction(
                nameof(Marketplace)
            );
        }


        // =========================================================
        // MEMBER - EDIT LISTING
        // =========================================================

        // GET: Listings/Edit/5
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Edit(int? id)
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
                        l => l.ListingId == id
                    );


            if (listing == null)
            {
                return NotFound();
            }


            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );


            // Members may edit only their own listings.
            if (listing.MemberId != userId)
            {
                return Forbid();
            }


            // Approved listings cannot be edited by Members.
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
                        l => l.ListingId == id
                    );


            if (existingListing == null)
            {
                return NotFound();
            }


            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );


            // Members may edit only their own listings.
            if (existingListing.MemberId != userId)
            {
                return Forbid();
            }


            // Approved listings cannot be edited.
            if (existingListing.Status ==
                ListApprovalStatus.Approved)
            {
                return Forbid();
            }


            // Remember whether the listing was previously removed.
            bool wasRemoved =
                existingListing.Status ==
                ListApprovalStatus.Removed;


            // -----------------------------------------------------
            // FORM VALIDATION
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                // Keep existing image information available
                // when redisplaying the Edit form.
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
            // REMOVED LISTING RESUBMISSION
            // -----------------------------------------------------

            // Editing a previously removed listing sends it
            // back to administrators for another review.
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
                foreach (var imageId in DeletedImageIds)
                {
                    var image =
                        existingListing.Images
                            .FirstOrDefault(i =>
                                i.ListingImageId ==
                                imageId
                            );


                    if (image == null)
                    {
                        continue;
                    }


                    var filePath =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            image.ImagePath.TrimStart('/')
                        );


                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
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
                                ListingId = listingId,
                                ImagePath = path
                            },
                        "marketplace"
                    );


                _context.ListingImages
                    .AddRange(savedImages);
            }


            // -----------------------------------------------------
            // SAVE CHANGES
            // -----------------------------------------------------

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // NOTIFY ADMINS IF REMOVED LISTING WAS RESUBMITTED
            // -----------------------------------------------------

            if (wasRemoved)
            {
                var admins =
                    await _userManager
                        .GetUsersInRoleAsync("Admin");


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
                            NotificationType.NewMarketplaceSubmission,
                            notificationTitle,
                            notificationMessage,
                            existingListing.Images
                                .FirstOrDefault()
                                ?.ImagePath,
                            "/Listings/Details/" +
                            existingListing.ListingId,
                            listingId:
                                existingListing.ListingId
                        );
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
                }
            );
        }


        // =========================================================
        // MEMBER - REMOVE SINGLE IMAGE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> RemoveImage(int id)
        {
            var image =
                await _context.ListingImages
                    .Include(i => i.Listing)
                    .FirstOrDefaultAsync(
                        i => i.ListingImageId == id
                    );


            if (image == null)
            {
                return NotFound();
            }


            var userId =
                _userManager.GetUserId(User);


            if (image.Listing.MemberId != userId)
            {
                return Forbid();
            }


            var listingId =
                await ImageHelper.RemoveImageAsync(
                    _context,
                    _context.ListingImages,
                    id,
                    img => img.ImagePath,
                    img => img.ListingId
                );


            return RedirectToAction(
                nameof(Edit),
                new
                {
                    id = listingId
                }
            );
        }


        // =========================================================
        // ADMIN - APPROVE LISTING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var listing =
                await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id
                    );


            if (listing == null)
            {
                return NotFound();
            }


            // Prevent repeated approval from creating
            // duplicate notifications.
            if (listing.Status ==
                ListApprovalStatus.Approved)
            {
                TempData["InfoMessage"] =
                    "This Marketplace listing is already approved.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // -----------------------------------------------------
            // APPROVE
            // -----------------------------------------------------

            listing.Status =
                ListApprovalStatus.Approved;


            await _context.SaveChangesAsync();


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
                    NotificationType.MarketplaceApproved,
                    notificationTitle,
                    notificationMessage,
                    listing.Images
                        .FirstOrDefault()
                        ?.ImagePath,
                    "/Listings/MarketplaceDetails/" +
                    listing.ListingId,
                    listingId:
                        listing.ListingId
                );


            TempData["SuccessMessage"] =
                "Marketplace listing approved successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // ADMIN - REJECT LISTING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var listing =
                await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(
                        l => l.ListingId == id
                    );


            if (listing == null)
            {
                return NotFound();
            }


            // Prevent repeated rejection from creating
            // duplicate notifications.
            if (listing.Status ==
                ListApprovalStatus.Rejected)
            {
                TempData["InfoMessage"] =
                    "This Marketplace listing is already rejected.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // -----------------------------------------------------
            // REJECT
            // -----------------------------------------------------

            listing.Status =
                ListApprovalStatus.Rejected;


            await _context.SaveChangesAsync();


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
                    NotificationType.MarketplaceRejected,
                    notificationTitle,
                    notificationMessage,
                    listing.Images
                        .FirstOrDefault()
                        ?.ImagePath,
                    "/Listings/MarketplaceDetails/" +
                    listing.ListingId,
                    listingId:
                        listing.ListingId
                );


            TempData["SuccessMessage"] =
                "Marketplace listing rejected successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }
    }
}