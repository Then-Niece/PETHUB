using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PETHUB.Controllers
{
    public class ListingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ListingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Listings
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Index()
            => RedirectToAction(nameof(Approvals));

        // One moderation page for Marketplace and Lost & Found, with shared filters.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approvals(string? statusFilter, string? petTypeFilter)
        {
            var listings = await _context.Listings
                .Include(l => l.Member)
                .Include(l => l.Images) // load related images
                .ToListAsync();
            var reports = await _context.LostFounds
                .Include(l => l.User)
                .Include(l => l.Images)
                .ToListAsync();

            if (Enum.TryParse<ListApprovalStatus>(statusFilter, true, out var listingStatus))
                listings = listings.Where(l => l.Status == listingStatus).ToList();
            if (Enum.TryParse<ApprovalStatus>(statusFilter, true, out var reportStatus))
                reports = reports.Where(l => l.Status == reportStatus).ToList();
            if (Enum.TryParse<ListPetType>(petTypeFilter, true, out var listingPetType))
                listings = listings.Where(l => l.PetType == listingPetType).ToList();
            if (Enum.TryParse<PetType>(petTypeFilter, true, out var reportPetType))
                reports = reports.Where(l => l.PetType == reportPetType).ToList();

            return View(new ApprovalDashboardViewModel
            {
                Listings = listings,
                LostFounds = reports,
                StatusFilter = statusFilter,
                PetTypeFilter = petTypeFilter
            });
        }

        // GET: Marketplace Listing for Client and Member
        [AllowAnonymous]
        public async Task<IActionResult> Marketplace(string? petTypeFilter)
        {
            var listings = await _context.Listings
               .Include(l => l.Member)
               .Where(l => l.Status == ListApprovalStatus.Approved)
               .Include(l => l.Images) // load related images
               .ToListAsync();
            if (Enum.TryParse<ListPetType>(petTypeFilter, true, out var petType))
                listings = listings.Where(l => l.PetType == petType).ToList();

            return View(listings);
        }


        // GET: Listings/Details/AdminView
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings
                .Include(l => l.Member)    // keep user info
                .Include(l => l.Images)  // load related images
                // Visitors and members cannot enumerate pending or rejected listings by ID.
                .FirstOrDefaultAsync(m => m.ListingId == id && m.Status == ListApprovalStatus.Approved);
            if (listing == null)
            {
                return NotFound();
            }

            return View(listing);
        }


        // GET: Listings/Details/For Client and Member
        [AllowAnonymous]
        public async Task<IActionResult> MarketplaceDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings
                .Include(l => l.Member)    // keep user info
                .Include(l => l.Images)  // load related images
                .FirstOrDefaultAsync(m => m.ListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            return View(listing);
        }

        // GET: Listings/Create
        [Authorize(Roles = "Member")]
        public IActionResult Create()
        {
            return View();
        }


        // POST: Listings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Create(Listing listing, List<IFormFile> ImageFiles)
        {
            if (ModelState.IsValid)
            {
                listing.DatePosted = DateTime.Now;
                // The server, rather than a hidden form field, assigns ownership.
                listing.MemberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Location is automatically bound from the form
                _context.Add(listing);
                await _context.SaveChangesAsync();
                if (ImageFiles != null && ImageFiles.Count > 0)
                {
                    var savedImages = await ImageUploadHelper.SaveImagesAsync(
                        ImageFiles,
                        listing.ListingId,
                        (id, path) => new ListingImage { ListingId = id, ImagePath = path },
                        "marketplace"
                    );

                    _context.AddRange(savedImages);
                    await _context.SaveChangesAsync();
                }

                //fixed. this redirects now to the marketplace method that only member and client can access
                return RedirectToAction(nameof(Marketplace));
            }
            return View(listing);
        }




        // GET: Listings/Edit/5
        [Authorize(Roles = "Member,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.ListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!User.IsInRole("Admin") && listing.MemberId != userId)
            {
                return Forbid();
            }

            return View(listing);
        }


        // POST: Listings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member,Admin")]
        public async Task<IActionResult> Edit(int id, Listing listing, List<IFormFile> ImageFiles)
        {
            if (id != listing.ListingId)
            {
                return NotFound();
            }

            var existingListing = await _context.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.ListingId == id);

            if (existingListing == null)
            {
                return NotFound();
            }

            // Get the currently logged-in user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Admin can edit any listing.
            // Member can only edit their own listing.
            if (!User.IsInRole("Admin") && existingListing.MemberId != userId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    foreach (var error in item.Value.Errors)
                    {
                        Console.WriteLine($"{item.Key}: {error.ErrorMessage}");
                    }
                }

                ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", existingListing.MemberId);
                return View(existingListing);
            }

            // Update fields
            existingListing.Title = listing.Title;
            existingListing.Description = listing.Description;
            existingListing.Price = listing.Price;
            existingListing.Location = listing.Location;
            existingListing.DatePosted = DateTime.Now;
            existingListing.Breed = listing.Breed;
            existingListing.PetType = listing.PetType;
            existingListing.PetSex = listing.PetSex;
            existingListing.Type = listing.Type;

            // Handle new images
            if (ImageFiles != null && ImageFiles.Count > 0)
            {
                var savedImages = await ImageUploadHelper.SaveImagesAsync(
                    ImageFiles,
                    existingListing.ListingId,
                    (listingId, path) => new ListingImage
                    {
                        ListingId = listingId,
                        ImagePath = path
                    },
                    "marketplace"
                );

                _context.AddRange(savedImages);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(User.IsInRole("Admin") ? nameof(Approvals) : nameof(Marketplace));
        }






        // GET: Listings/Delete/5

        [Authorize(Roles = "Member, Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings
                .Include(l => l.Member)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(m => m.ListingId == id);
            if (listing == null)
            {
                return NotFound();
            }
            //I added this to ensure that only the owner of the listing or an admin can delete it
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!User.IsInRole("Admin") && listing.MemberId != userId)
            {
                return Forbid();
            }

            return View(listing);
        }

        // POST: Listings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Images) // include related images
                .FirstOrDefaultAsync(l => l.ListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Admin can delete any listing.
            // Member can only delete their own listing.
            if (!User.IsInRole("Admin") && listing.MemberId != userId)
            {
                return Forbid();
            }

            // Delete image files from wwwroot/images
            if (listing.Images != null && listing.Images.Any())
            {
                foreach (var img in listing.Images)
                {
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        img.ImagePath.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    _context.ListingImages.Remove(img);
                }
            }

            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();

            return RedirectToAction(User.IsInRole("Admin") ? nameof(Approvals) : nameof(Marketplace));
        }

        // POST: Listings/EDIT/REMOVEIMAGE/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member,Admin")]
        public async Task<IActionResult> RemoveImage(int id)
        {
            var image = await _context.ListingImages.Include(i => i.Listing).FirstOrDefaultAsync(i => i.ListingImageId == id);
            if (image != null)
            {
                if (!User.IsInRole("Admin") && image.Listing?.MemberId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                    return Forbid();
                // Delete file from wwwroot/images
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Remove from DB
                _context.ListingImages.Remove(image);
                await _context.SaveChangesAsync();

                // Redirect back to Edit view of the listing
                return RedirectToAction("Edit", new { id = image.ListingId });
            }

            return NotFound();
        }

        // GET: Listings/Approve
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var report = await _context.Listings.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = ListApprovalStatus.Approved;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Approvals));
        }

        // GET: Listings/Reject
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var report = await _context.Listings.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = ListApprovalStatus.Rejected;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Approvals));
        }

        // GET: Listings/Sold
        [HttpPost]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Sold(int id)
        {
            var report = await _context.Listings.FindAsync(id);
            if (report == null) return NotFound();

            if (report.MemberId != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();

            report.ListStatus = ListingStatus.Sold;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Marketplace));
        }

        // GET: Listings/Adopted
        [HttpPost]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Adopted(int id)
        {
            var report = await _context.Listings.FindAsync(id);
            if (report == null) return NotFound();

            if (report.MemberId != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();

            report.ListStatus = ListingStatus.Adopted;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Marketplace));
        }

        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.ListingId == id);
        }
    }
}
