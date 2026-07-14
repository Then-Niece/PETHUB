using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;
using System.Threading.Tasks;

namespace PETHUB.Controllers
{
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel();

            // --- 1. OVERVIEW ---
            // Fetch users based on roles exactly as done in your Members/Users controllers
            var members = await _userManager.GetUsersInRoleAsync("Member");
            viewModel.TotalMembers = members.Count;

            var users = await _userManager.GetUsersInRoleAsync("User"); // Represents Admins per your architecture
            viewModel.TotalUsers = users.Count;

            // Participants definition (Customize this logic based on your project's strict definition)
            viewModel.TotalParticipants = viewModel.TotalMembers + viewModel.TotalUsers;

            // --- 2. PENDING APPROVALS ---
            // Counts items awaiting approval using your existing ApprovalStatus enum
            //viewModel.PendingMarketplaceListings = await _context.Listings
            //    .CountAsync(l => l.Status == ApprovalStatus.Pending);

            viewModel.PendingLostAndFoundPosts = await _context.LostFounds
                .CountAsync(lf => lf.Status == ApprovalStatus.Pending);

            // --- 3. PENDING REPORTS ---
            // Note: Replace these placeholders once your Report tables are implemented in ApplicationDbContext
            viewModel.PendingMarketplaceReports = 0; // e.g., await _context.MarketplaceReports.CountAsync(...)
            viewModel.PendingLostAndFoundReports = 0; // e.g., await _context.LostFoundReports.CountAsync(...)

            return View(viewModel);
        }
    }
}