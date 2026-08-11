using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    // This controller is restricted to administrators.
    // The Authorize attribute prevents Members or unauthenticated users
    // from directly accessing the Admin Dashboard URL.
    [Authorize(Roles = "Dennis")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Dependency Injection provides the database context and Identity
        // UserManager. The controller uses these services to retrieve
        // platform statistics without creating database/service objects manually.
        public AdminDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /AdminDashboard
        //
        // This action only gathers dashboard information.
        // It does NOT approve or reject Marketplace/Lost & Found posts.
        // The Approval Queue simply uses the resulting pending counts
        // and the View will later redirect administrators to the existing
        // approval management pages.
        public async Task<IActionResult> Index()
        {
            // ---------------------------------------------------------
            // COUNT MEMBERS AND ADMINISTRATORS
            // ---------------------------------------------------------

            // ASP.NET Identity stores both Members and Administrators
            // inside ApplicationUser. Therefore, we cannot simply count
            // all users and call that the number of Members.
            //
            // GetUsersInRoleAsync() retrieves users assigned to the
            // specified Identity role, allowing the dashboard to count
            // Members and Admins separately.
            var members = await _userManager.GetUsersInRoleAsync("Member");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            // ---------------------------------------------------------
            // MARKETPLACE STATISTICS
            // ---------------------------------------------------------

            // CountAsync() asks Entity Framework Core to count the
            // matching records directly in the database rather than
            // loading every Listing into application memory.
            var totalMarketplaceListings =
                await _context.Listings.CountAsync();

            // The Listing model uses ListApprovalStatus to represent
            // administrator approval: Pending, Approved, or Rejected.
            var marketplacePending =
                await _context.Listings.CountAsync(
                    listing => listing.Status == ListApprovalStatus.Pending);

            var marketplaceApproved =
                await _context.Listings.CountAsync(
                    listing => listing.Status == ListApprovalStatus.Approved);

            var marketplaceRejected =
                await _context.Listings.CountAsync(
                    listing => listing.Status == ListApprovalStatus.Rejected);


            // ---------------------------------------------------------
            // LOST & FOUND STATISTICS
            // ---------------------------------------------------------

            // Count every Lost & Found report stored in the database.
            // This count is separate from ReportStatus because the
            // dashboard's total represents all reports in the system.
            var totalLostFoundReports =
                await _context.LostFounds.CountAsync();

            // Lost & Found uses ApprovalStatus for administrator approval.
            // ReportStatus (Active/Resolved) is intentionally not used
            // for these three Platform Summary values.
            var lostFoundPending =
                await _context.LostFounds.CountAsync(
                    report => report.Status == ApprovalStatus.Pending);

            var lostFoundApproved =
                await _context.LostFounds.CountAsync(
                    report => report.Status == ApprovalStatus.Approved);

            var lostFoundRejected =
                await _context.LostFounds.CountAsync(
                    report => report.Status == ApprovalStatus.Rejected);


            // ---------------------------------------------------------
            // PETFEED STATISTICS
            // ---------------------------------------------------------

            // PetFeed posts are administrator-created content and do not
            // use the same Marketplace/Lost & Found approval workflow.
            // Therefore, the dashboard only needs the total number of posts.
            var totalPetFeedPosts =
                await _context.PetFeeds.CountAsync();


            // ---------------------------------------------------------
            // BUILD DASHBOARD VIEWMODEL
            // ---------------------------------------------------------

            // All database results are placed into one ViewModel.
            // The Razor view will use these properties to display the
            // statistics, pie charts, and approval queue.
            var viewModel = new AdminDashboardViewModel
            {
                // Identity role counts.
                TotalMembers = members.Count,
                TotalAdmins = admins.Count,

                // Overall platform counts.
                TotalMarketplaceListings = totalMarketplaceListings,
                TotalLostFoundReports = totalLostFoundReports,
                TotalPetFeedPosts = totalPetFeedPosts,

                // Marketplace approval summary.
                MarketplacePending = marketplacePending,
                MarketplaceApproved = marketplaceApproved,
                MarketplaceRejected = marketplaceRejected,

                // Lost & Found approval summary.
                LostFoundPending = lostFoundPending,
                LostFoundApproved = lostFoundApproved,
                LostFoundRejected = lostFoundRejected
            };

            // Return the populated ViewModel to
            // Views/AdminDashboard/Index.cshtml.
            return View(viewModel);
        }
    }
}