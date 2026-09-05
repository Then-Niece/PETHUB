using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailSender _emailSender;


        public AdminsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: Admins
        public async Task<IActionResult> Index(
            string? search,
            int page = 1)
        {
            // Get the current logged-in user's ID so they are not
            // displayed in the administrator management table.
            var currentUserId = _userManager.GetUserId(User);


            // =========================================================
            // GET ADMINISTRATORS
            // =========================================================

            var users = (await _userManager.GetUsersInRoleAsync("Admin"))
                .Where(u => u.Id != currentUserId)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();


            // =========================================================
            // SEARCH
            // =========================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                users = users
                    .Where(u =>
                        (!string.IsNullOrWhiteSpace(u.UserName) &&
                         u.UserName.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        (
                            $"{u.FirstName} {u.LastName}"
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        ) ||

                        (!string.IsNullOrWhiteSpace(u.FirstName) &&
                         u.FirstName.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrWhiteSpace(u.LastName) &&
                         u.LastName.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrWhiteSpace(u.Email) &&
                         u.Email.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrWhiteSpace(u.ContactNumber) &&
                         u.ContactNumber.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        u.Status
                            .ToString()
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
            }


            // Keep the search text after the page reloads.
            ViewBag.Search = search;


            // =========================================================
            // PAGINATION
            // =========================================================

            const int pageSize = 25;

            // Prevent invalid page numbers.
            if (page < 1)
            {
                page = 1;
            }

            // TotalItems now represents the number of matching admins
            // when a search is active.
            var totalItems = users.Count;


            var totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize
            );


            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }


            // Get only the administrators for the current page.
            var pagedUsers = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            // =========================================================
            // ROLE DICTIONARY
            // =========================================================

            var userRoles = new Dictionary<string, string>();

            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userRoles[user.Id] =
                    roles.FirstOrDefault() ?? "No Role";
            }

            ViewBag.UserRoles = userRoles;


            // =========================================================
            // PAGINATION VIEWMODEL
            // =========================================================

            var model = new PaginationViewModel<ApplicationUser>
            {
                Items = pagedUsers,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };


            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> Search(string? search)
        {
            // Get the current logged-in administrator so they remain
            // excluded from the administrator management list.
            var currentUserId = _userManager.GetUserId(User);


            // =========================================================
            // GET ADMINISTRATORS
            // =========================================================

            var admins = (await _userManager.GetUsersInRoleAsync("Admin"))
                .Where(a => a.Id != currentUserId)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();


            // =========================================================
            // SEARCH
            // =========================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                admins = admins
                    .Where(a =>
                        (!string.IsNullOrWhiteSpace(a.UserName) &&
                         a.UserName.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        ($"{a.FirstName} {a.LastName}"
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrWhiteSpace(a.FirstName) &&
                         a.FirstName.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrWhiteSpace(a.LastName) &&
                         a.LastName.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrWhiteSpace(a.Email) &&
                         a.Email.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrWhiteSpace(a.ContactNumber) &&
                         a.ContactNumber.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)) ||

                        a.Status
                            .ToString()
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
            }


            // =========================================================
            // ROLE DICTIONARY
            // =========================================================

            var userRoles = new Dictionary<string, string>();

            foreach (var admin in admins)
            {
                var roles = await _userManager.GetRolesAsync(admin);

                userRoles[admin.Id] =
                    roles.FirstOrDefault() ?? "No Role";
            }

            ViewBag.UserRoles = userRoles;


            return PartialView("_AdminRows", admins);
        }


        // GET: Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationUser = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (applicationUser == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(applicationUser);
            ViewBag.UserRoles = new Dictionary<string, string>
    {
        { applicationUser.Id, roles.FirstOrDefault() ?? "No Role" }
    };

            return View(applicationUser);
        }


        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminInvitationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Identity already checks the email

            // ==========================================
            // CREATE PENDING USER
            // ==========================================

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,

                Status = UserStatus.Pending,

                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }


            // ==========================================
            // ASSIGN ADMIN ROLE
            // ==========================================

            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                // Remove the pending user if role assignment fails
                await _userManager.DeleteAsync(user);

                return View(model);
            }


            // ==========================================
            // GENERATE INVITATION TOKEN
            // ==========================================

            var token = await _userManager.GenerateUserTokenAsync(
                user,
                "PETHubAdminInvitation",
                "AdminInvitation");



            // ==========================================
            // CREATE INVITATION LINK
            // ==========================================

            var invitationLink = Url.Action(
                "AdminSetup",
                "UserAccount",
                new
                {
                    userId = user.Id,
                    token = token
                },
                Request.Scheme);


            // ==========================================
            // CREATE EMAIL
            // ==========================================

            var emailBody = Helpers.EmailTemplateHelper.AdminInvitation(
                invitationLink);


            // ==========================================
            // SEND EMAIL
            // ==========================================

            await _emailSender.SendEmailAsync(
                user.Email,
                "PETHUB Administrator Invitation",
                emailBody);


            // ==========================================
            // DONE
            // ==========================================

            TempData["SuccessMessage"] =
                "Administrator invitation sent successfully.";

            return RedirectToAction(nameof(Index));
        }




        // =========================================================
        // DEACTIVATE ADMIN
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            if (id == currentUserId)
            {
                TempData["WarningMessage"] =
                    "You cannot deactivate your own administrator account.";

                return RedirectToAction(nameof(Index));
            }

            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            var isAdmin = await _userManager.IsInRoleAsync(admin, "Admin");

            if (!isAdmin)
            {
                return NotFound();
            }

            if (admin.Status == UserStatus.Inactive)
            {
                return RedirectToAction(nameof(Index));
            }

            admin.Status = UserStatus.Inactive;

            var result = await _userManager.UpdateAsync(admin);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Unable to deactivate the administrator account.";

                return RedirectToAction(nameof(Index));
            }

            await _userManager.UpdateSecurityStampAsync(admin);

            TempData["SuccessMessage"] =
                "Administrator account has been deactivated.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // REACTIVATE ADMIN
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            var isAdmin = await _userManager.IsInRoleAsync(admin, "Admin");

            if (!isAdmin)
            {
                return NotFound();
            }

            if (admin.Status == UserStatus.Active)
            {
                return RedirectToAction(nameof(Index));
            }

            admin.Status = UserStatus.Active;

            var result = await _userManager.UpdateAsync(admin);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Unable to reactivate the administrator account.";

                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "Administrator account has been reactivated.";

            return RedirectToAction(nameof(Index));
        }

    }
}
