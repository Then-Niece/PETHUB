using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;

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
        public async Task<IActionResult> Index()
        {
            // Retrieves the currently authenticated ApplicationUser.
            var user = await _userManager.GetUserAsync(User);

            // Stop if the authenticated user cannot be found.
            if (user == null)
            {
                return Unauthorized();
            }

            // Admins can see all Admin activity.
            if (User.IsInRole("Admin"))
            {
                // Retrieves all logs created by Admin accounts.
                // The newest activity appears first.
                var adminLogs = await _context.AuditLogs
                    .Where(log => log.Role == "Admin")
                    .OrderByDescending(log => log.CreatedAt)
                    .ToListAsync();

                // Retrieves the Identity users whose IDs appear in the Admin logs.
                // ToListAsync executes the query and loads the users into memory.
                var adminUserIds = adminLogs
                    .Select(log => log.UserId)
                    .Distinct()
                    .ToList();

                var adminUsers = await _userManager.Users
                    .Where(u => adminUserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Sends both the logs and matching Admin users to the View.
                ViewBag.AdminUsers = adminUsers;

                return View(adminLogs);
            }

            // Members can only see their own activity.
            var memberLogs = await _context.AuditLogs
                .Where(log => log.UserId == user.Id)
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync();

            return View(memberLogs);
        }
    }
}