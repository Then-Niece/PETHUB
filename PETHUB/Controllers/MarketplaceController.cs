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
    public class MarketplaceController : Controller
    {

        private readonly ApplicationDbContext _context;

        public MarketplaceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Marketplace Listing for Client POV
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
    }
}
