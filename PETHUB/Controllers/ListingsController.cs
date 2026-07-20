using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        {
            var listings = await _context.Listings
                .Include(l => l.Member)
                .Include(l => l.Images) // load related images
                .ToListAsync();

            return View(listings);
        }

        // GET: Marketplace Listing for Client POV
        public async Task<IActionResult> Marketplace()
        {
            var listings = await _context.Listings
               .Include(l => l.Member)
               .Where(l => l.Status == ListApprovalStatus.Approved)
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


        // GET: Listings/Details/For Client POV
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
        public IActionResult Create()
        {
            return View();
        }


        // POST: Listings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Listing listing, List<IFormFile> ImageFiles)
        {
            if (ModelState.IsValid)
            {
                listing.DatePosted = DateTime.Now;

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


                return RedirectToAction(nameof(Marketplace));
            }
            return View(listing);
        }







        // GET: Listings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings
                .Include(l => l.Images) // load related images
                .Include(l => l.Member)   // optional: keep user info if needed
                .FirstOrDefaultAsync(l => l.ListingId == id);
            if (listing == null)
            {
                return NotFound();
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", listing.MemberId);
            return View(listing);
        }


        // POST: Listings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Listing listing, List<IFormFile> ImageFiles)
        {
            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    foreach (var error in item.Value.Errors)
                    {
                        Console.WriteLine($"{item.Key}: {error.ErrorMessage}");
                    }
                }
            }
            if (id != listing.ListingId) return NotFound();

            if (ModelState.IsValid)
            {
                var existingListing = await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(l => l.ListingId == id);

                if (existingListing == null) return NotFound();

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
                        (id, path) => new ListingImage { ListingId = id, ImagePath = path },
                        "marketplace"
                    );

                    _context.AddRange(savedImages);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(listing);
        }






        // GET: Listings/Delete/5
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

            return View(listing);
        }

        // POST: Listings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Images) // include related images
                .FirstOrDefaultAsync(l => l.ListingId == id);

            if (listing != null)
            {
                // Delete image files from wwwroot/images
                if (listing.Images != null && listing.Images.Any())
                {
                    foreach (var img in listing.Images)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                        _context.ListingImages.Remove(img);
                    }
                }

                _context.Listings.Remove(listing);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Listings/EDIT/REMOVEIMAGE/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveImage(int id)
        {
            var image = await _context.ListingImages.FindAsync(id);
            if (image != null)
            {
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
        public async Task<IActionResult> Reject(int id)
        {
            var report = await _context.Listings.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = ListApprovalStatus.Rejected;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Listings/Sold
        [HttpPost]
        public async Task<IActionResult> Sold(int id)
        {
            var report = await _context.Listings.FindAsync(id);
            if (report == null) return NotFound();

            report.ListStatus = ListingStatus.Sold;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Listings/Adopted
        [HttpPost]
        public async Task<IActionResult> Adopted(int id)
        {
            var report = await _context.Listings.FindAsync(id);
            if (report == null) return NotFound();

            report.ListStatus = ListingStatus.Adopted;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.ListingId == id);
        }
    }
}
