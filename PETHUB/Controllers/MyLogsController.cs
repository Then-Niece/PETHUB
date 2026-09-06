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
        [HttpGet]
        public async Task<IActionResult> Index(
    string? search,
    string? category,
    string? month,
    int page = 1)
        {
            // =========================================================
            // CURRENT USER
            // =========================================================

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }


            // =========================================================
            // PAGINATION
            // =========================================================

            const int pageSize = 25;

            if (page < 1)
            {
                page = 1;
            }


            // Clean filter values.
            search = search?.Trim();
            category = category?.Trim();
            month = month?.Trim();


            // =========================================================
            // SAVE CURRENT FILTERS FOR THE VIEW
            // =========================================================

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Month = month;


            // =========================================================
            // BASE QUERY
            // =========================================================

            IQueryable<AuditLog> query =
                _context.AuditLogs;


            // Admin sees all Admin logs.
            if (User.IsInRole("Admin"))
            {
                query =
                    query.Where(log =>
                        log.Role == "Admin");
            }
            else
            {
                // Members only see their own logs.
                query =
                    query.Where(log =>
                        log.UserId == user.Id);
            }


            // =========================================================
            // SEARCH
            //
            // Admin:
            // - Admin first name
            // - Admin middle name
            // - Admin last name
            // - Action
            //
            // Member:
            // - Action
            // =========================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                if (User.IsInRole("Admin"))
                {
                    // Find Admin IDs whose names match the search.
                    var matchingAdminIds =
                        await _userManager.Users
                            .Where(u =>
                                (u.FirstName != null &&
                                 u.FirstName.Contains(search))
                                ||
                                (u.MiddleName != null &&
                                 u.MiddleName.Contains(search))
                                ||
                                (u.LastName != null &&
                                 u.LastName.Contains(search))
                                ||
                                (
                                    (
                                        (u.FirstName ?? "") + " " +
                                        (u.MiddleName ?? "") + " " +
                                        (u.LastName ?? "")
                                    ).Contains(search)
                                )
                            )
                            .Select(u => u.Id)
                            .ToListAsync();


                    query =
                        query.Where(log =>
                            log.Action.Contains(search)
                            ||
                            matchingAdminIds.Contains(log.UserId));
                }
                else
                {
                    query =
                        query.Where(log =>
                            log.Action.Contains(search));
                }
            }


            // =========================================================
            // CATEGORY FILTER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(category))
            {
                switch (category.ToLower())
                {
                    // -----------------------------------------------------
                    // LOGGED
                    // -----------------------------------------------------

                    case "logged":

                        query =
                            query.Where(log =>
                                log.Action == "Logged In"
                                ||
                                log.Action == "Logged Out");

                        break;


                    // -----------------------------------------------------
                    // POSTS
                    //
                    // Examples:
                    // Created Post
                    // Edited Post
                    // Deleted Post
                    // Approved Post
                    // Rejected Post
                    // -----------------------------------------------------

                    case "posts":

                        query =
                            query.Where(log =>
                                log.Action.Contains("Post"));

                        break;


                    // -----------------------------------------------------
                    // REPORTS
                    //
                    // Supports moderation activity such as:
                    // Appeal Approved
                    // Appeal Rejected
                    // Violation Approved
                    // Report Dismissed
                    // etc.
                    // -----------------------------------------------------

                    case "reports":

                        query =
                            query.Where(log =>
                                log.Action.Contains("Report")
                                ||
                                log.Action.Contains("Appeal")
                                ||
                                log.Action.Contains("Violation")
                                ||
                                log.Action.Contains("Dismiss"));

                        break;
                }
            }


            // =========================================================
            // MONTH FILTER
            //
            // Example:
            //
            // month = "2026-09"
            //
            // Philippine date range:
            // September 1, 2026 12:00 AM
            // through
            // October 1, 2026 12:00 AM
            //
            // AuditLog timestamps are UTC, so Philippine UTC+8
            // boundaries are converted to UTC before querying.
            // =========================================================

            if (!string.IsNullOrWhiteSpace(month))
            {
                if (DateTime.TryParseExact(
                    month,
                    "yyyy-MM",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var selectedMonth))
                {
                    var localStart =
                        new DateTime(
                            selectedMonth.Year,
                            selectedMonth.Month,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Unspecified);

                    var localEnd =
                        localStart.AddMonths(1);


                    // Philippines is UTC+8.
                    var startUtc =
                        DateTime.SpecifyKind(
                            localStart - TimeSpan.FromHours(8),
                            DateTimeKind.Utc);

                    var endUtc =
                        DateTime.SpecifyKind(
                            localEnd - TimeSpan.FromHours(8),
                            DateTimeKind.Utc);


                    query =
                        query.Where(log =>
                            log.CreatedAt >= startUtc
                            &&
                            log.CreatedAt < endUtc);
                }
            }


            // =========================================================
            // SORT
            // =========================================================

            query =
                query.OrderByDescending(log =>
                    log.CreatedAt);


            // =========================================================
            // COUNT AFTER FILTERING
            // =========================================================

            var totalItems =
                await query.CountAsync();


            var totalPages =
                (int)Math.Ceiling(
                    totalItems / (double)pageSize);


            if (totalPages > 0 &&
                page > totalPages)
            {
                page = totalPages;
            }


            // =========================================================
            // CURRENT PAGE
            // =========================================================

            var logs =
                await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();


            // =========================================================
            // ADMIN INFORMATION
            //
            // Needed for:
            // - Admin name
            // - Profile picture
            // =========================================================

            if (User.IsInRole("Admin"))
            {
                var adminUserIds =
                    logs
                        .Select(log => log.UserId)
                        .Distinct()
                        .ToList();


                var adminUsers =
                    await _userManager.Users
                        .Where(u =>
                            adminUserIds.Contains(u.Id))
                        .ToDictionaryAsync(
                            u => u.Id);


                ViewBag.AdminUsers =
                    adminUsers;
            }


            // =========================================================
            // MODEL
            // =========================================================

            var model =
                new PaginationViewModel<AuditLog>
                {
                    Items = logs,

                    CurrentPage = page,

                    PageSize = pageSize,

                    TotalItems = totalItems
                };


            return View(model);
        }
    }
}