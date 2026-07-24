using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;
using System.Diagnostics;

namespace PETHUB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Marketplace", "Listings");
            }

            var latestListings = await _context.Listings
                .Include(l => l.Images)
                .Where(l => l.Status == ListApprovalStatus.Approved)
                .OrderByDescending(l => l.DatePosted)
                .Take(4)
                .ToListAsync();

            var latestLostFound = await _context.LostFounds
                .Include(l => l.Images)
                .Where(l => l.Status == ApprovalStatus.Approved)
                .OrderByDescending(l => l.DateReported)
                .Take(4)
                .ToListAsync();

            var viewModel = new PublicLandingPageViewModel
            {
                MarketplaceListings = latestListings,
                LostFoundReports = latestLostFound
            };

            return View("PublicLandingPage", viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}