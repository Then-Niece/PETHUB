using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
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

        // GET: Users
        public async Task<IActionResult> Index(int page = 1)
        {
            // get the current logged-in user's ID to exclude them from the list
            var currentUserId = _userManager.GetUserId(User);

            // Get all users in the "Admin" role, excluding the current logged-in user, and order them by CreatedAt descending
            var users = (await _userManager.GetUsersInRoleAsync("Admin"))
                .Where(u => u.Id != currentUserId)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();


            // =========================================================
            // PAGINATION
            // =========================================================

            const int pageSize = 10;

            // Prevent invalid page numbers.
            if (page < 1)
            {
                page = 1;
            }

            // Get the total number of admins before pagination.
            var totalItems = users.Count;

            // Prevent the requested page from going beyond
            // the available number of pages.
            var totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize
            );

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Get only the admins for the current page.
            var pagedUsers = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            // =========================================================
            // EXISTING ROLE DICTIONARY
            // =========================================================

            // Build dictionary of user roles
            var userRoles = new Dictionary<string, string>();

            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "No Role";
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








        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationUser = await _context.Users.FindAsync(id);
            if (applicationUser == null)
            {
                return NotFound();
            }
            return View(applicationUser);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Update only allowed fields for Admins
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.ContactNumber = model.ContactNumber;
                user.Status = model.Status;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }


        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationUser = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationUser == null)
            {
                return NotFound();
            }

            return View(applicationUser);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var applicationUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (applicationUser == null)
            {
                return NotFound();
            }

            // ==========================================
            // DELETE NOTIFICATIONS BELONGING TO USER
            // ==========================================

            var notifications = await _context.Notifications
                .Where(n => n.UserId == id)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);


            // ==========================================
            // DELETE REPORTS SUBMITTED BY USER
            // ==========================================

            var reports = await _context.UserReports
                .Where(r => r.ReporterId == id)
                .ToListAsync();

            _context.UserReports.RemoveRange(reports);


            // ==========================================
            // DELETE USER
            // ==========================================

            _context.Users.Remove(applicationUser);


            // ==========================================
            // SAVE
            // ==========================================

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }





        private bool ApplicationUserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
