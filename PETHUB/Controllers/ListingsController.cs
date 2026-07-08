using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;

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
        public async Task<IActionResult> Index()
        {
            var listings = await _context.Listings
                .Include(l => l.User)
                .Include(l => l.Images) // load related images
                .ToListAsync();

            return View(listings);
        }


        // GET: Listings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings
                .Include(l => l.User)    // keep user info
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

        /*
    NOTE: Simplified Admin POV version of ListingsController

    - UserId handling:
        Normally, Listing.UserId must match a valid ApplicationUser.Id (GUID string).
        Here we hardcoded "Admin" or removed binding to avoid foreign key errors.
        This means listings are not linked to real users yet.

    - DatePosted:
        Auto-set in the controller (DateTime.Now) instead of requiring manual input.
        This avoids validation blocking when the field is left empty.

    - Bind attribute:
        Removed [Bind("Title,Description,Price")] restriction so EF can accept all fields.
        Otherwise, ModelState would fail because required fields weren’t included.

    - Identity integration:
        Cut out ViewBag.UserId dropdown and foreign key enforcement.
        This is temporary until login/roles are implemented.
        For now, listings exist without a valid ApplicationUser reference.

    TL;DR:
        Cuts were made to bypass ASP.NET Identity requirements and keep CRUD working
        for demo purposes. Once authentication is added, restore UserId binding and
        link listings to actual users.
*/



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

                // Save images
                if (ImageFiles != null && ImageFiles.Count > 0)
                {
                    foreach (var file in ImageFiles)
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var filePath = Path.Combine("wwwroot/images", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var listingImage = new ListingImage
                        {
                            ListingId = listing.ListingId,
                            ImagePath = "/images/" + fileName
                        };

                        _context.Add(listingImage);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
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
                .Include(l => l.User)   // optional: keep user info if needed
                .FirstOrDefaultAsync(l => l.ListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", listing.UserId);
            return View(listing);
        }


        // POST: Listings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Listing listing, List<IFormFile> ImageFiles)
        {
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

                // Handle new images
                if (ImageFiles != null && ImageFiles.Count > 0)
                {
                    foreach (var file in ImageFiles)
                    {
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                        if (!Directory.Exists(uploadDir))
                            Directory.CreateDirectory(uploadDir);

                        // Generate unique filename using GUID
                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadDir, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var listingImage = new ListingImage
                        {
                            ListingId = existingListing.ListingId,
                            ImagePath = "/images/" + uniqueFileName
                        };

                        _context.Add(listingImage);
                    }
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
                .Include(l => l.User)
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
                .Include(l => l.Images) // ✅ include related images
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


        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.ListingId == id);
        }
    }
}
