using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    // Only authenticated users can access the logs page.
    [Authorize]
    public class MyLogsController : Controller
    {
        // UserManager retrieves the currently logged-in Identity user.
        private readonly UserManager<ApplicationUser> _userManager;

        // ApplicationDbContext provides access to the AuditLogs table.
        private readonly ApplicationDbContext _context;

        // Dependency Injection supplies the required Identity and database services.
        public MyLogsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Displays activity logs based on the role of the currently authenticated user.
        // Members see only their own logs, while Admins see all Admin activity.
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            // Retrieves the currently authenticated ApplicationUser.
            var user = await _userManager.GetUserAsync(User);

            // Stop if the authenticated user cannot be found.
            if (user == null)
            {
                return Unauthorized();
            }


            // =========================================================
            // PAGINATION SETTINGS
            // =========================================================

            const int pageSize = 25;

            if (page < 1)
            {
                page = 1;
            }


            // =========================================================
            // ADMIN LOGS
            // =========================================================

            if (User.IsInRole("Admin"))
            {
                // Retrieves all logs created by Admin accounts.
                // The newest activity appears first.
                var query = _context.AuditLogs
                    .Where(log => log.Role == "Admin")
                    .OrderByDescending(log => log.CreatedAt);


                // Total number of Admin logs.
                var totalItems = await query.CountAsync();


                // Retrieves only the logs needed for the current page.
                var adminLogs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();


                // Retrieves the Identity users whose IDs appear
                // in the Admin logs on the current page.
                var adminUserIds = adminLogs
                    .Select(log => log.UserId)
                    .Distinct()
                    .ToList();


                var adminUsers = await _userManager.Users
                    .Where(u => adminUserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);


                // Sends the matching Admin users to the View.
                ViewBag.AdminUsers = adminUsers;


                // Creates the same pagination model used
                // by the other PETHUB pages.
                var model = new PaginationViewModel<AuditLog>
                {
                    Items = adminLogs,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                };


                return View(model);
            }


            // =========================================================
            // MEMBER LOGS
            // =========================================================

            // Members can only see their own activity.
            var memberQuery = _context.AuditLogs
                .Where(log => log.UserId == user.Id)
                .OrderByDescending(log => log.CreatedAt);


            // Total number of logs belonging to the member.
            var memberTotalItems = await memberQuery.CountAsync();


            // Retrieves only the logs needed for the current page.
            var memberLogs = await memberQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            // Creates the pagination model for the member.
            var memberModel = new PaginationViewModel<AuditLog>
            {
                Items = memberLogs,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = memberTotalItems
            };


            return View(memberModel);
        }
    }
}