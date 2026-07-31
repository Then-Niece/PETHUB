using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using System.Security.Claims;

namespace PETHUB.Controllers
{
    public class ListingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ListingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Listings
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string status)
        {
            var listings = _context.Listings
                .Include(l => l.Member)
                .Include(l => l.Images) // load related images
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<ListApprovalStatus>(status, out var selectedStatus))
                {
                    listings = listings.Where(l => l.Status == selectedStatus);
                }


            }
            return View(await listings.ToListAsync());
        }

        // GET: Marketplace Listing for Client and Member
        [AllowAnonymous]
        public async Task<IActionResult> Marketplace()
        {
            var listings = await _context.Listings
               .Include(l => l.Member)
               .Where(l => l.Status == ListApprovalStatus.Approved && l.ListStatus == ListingStatus.Pending)
               .Include(l => l.Images) // load related images
               .ToListAsync();

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
                .FirstOrDefaultAsync(m => m.ListingId == id);
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
            if (!ModelState.IsValid)
                return View(listing);

            listing.DatePosted = DateTime.Now;
            listing.MemberId = _userManager.GetUserId(User);

            _context.Add(listing);
            await _context.SaveChangesAsync();

            if (ImageFiles != null && ImageFiles.Count > 0)
            {
                try
                {
                    var savedImages = await ImageHelper.SaveImagesAsync(
                        ImageFiles,
                        listing.ListingId,
                        (id, path) => new ListingImage { ListingId = id, ImagePath = path },
                        "marketplace"
                    );

                    _context.AddRange(savedImages);
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Some images could not be uploaded.");
                    return View(listing);
                }
            }

            return RedirectToAction(nameof(Marketplace));
        }





        // GET: Listings/Edit/5
        [Authorize(Roles = "Member")]
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

            // Get the ID of the currently logged-in user.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only the owner (or an admin) is allowed to edit this listing.
            if (!User.IsInRole("Admin") && listing.MemberId != userId)
            {
                return Forbid();
            }

            // Members are NOT allowed to edit an approved listing.
            // Pending and Rejected listings may still be edited.
            // Admins are exempt from this restriction.
            if (!User.IsInRole("Admin") &&
                listing.Status == ListApprovalStatus.Approved)
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
        [Authorize(Roles = "Member")]
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
            // Members can only edit their own listing.
            if (!User.IsInRole("Admin") && existingListing.MemberId != userId)
            {
                return Forbid();
            }

            // Even if someone manually submits the Edit form,
            // approved listings are locked and cannot be modified.
            // Admins are exempt.
            if (!User.IsInRole("Admin") &&
                existingListing.Status == ListApprovalStatus.Approved)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
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
                var savedImages = await ImageHelper.SaveImagesAsync(
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

            // Return the owner to the updated Marketplace Details page.
            return RedirectToAction(
                "MarketplaceDetails",
                "MyPosts",
                new { id = existingListing.ListingId });
        }


        // GET: Listings/Delete/5

        [Authorize(Roles = "Member")]
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
        [Authorize(Roles = "Member")]
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

            return RedirectToAction(nameof(Index));
        }

        // POST: Listings/EDIT/REMOVEIMAGE/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> RemoveImage(int id)
        {
            var image = await _context.ListingImages
                .Include(i => i.Listing)
                .FirstOrDefaultAsync(i => i.ListingImageId == id);

            if (image == null || image.Listing.MemberId != _userManager.GetUserId(User))
                return NotFound();

            var listingId = await ImageHelper.RemoveImageAsync(
                _context,
                _context.ListingImages,
                id,
                img => img.ImagePath,
                img => img.ListingId
            );
            return RedirectToAction("Edit", new { id = listingId });

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
            return RedirectToAction(nameof(Index));
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
            return RedirectToAction(nameof(Index));
        }
        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.ListingId == id);
        }
    }
}
