using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    public class MembersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // GET: Members
        public async Task<IActionResult> Index(int page = 1)
        {
            // Only get users in the Member role
            var members = (await _userManager.GetUsersInRoleAsync("Member"))
                .OrderByDescending(m => m.CreatedAt) // Newest first
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

            // Get the total number of members before pagination.
            var totalItems = members.Count;

            // Prevent the requested page from going beyond
            // the available number of pages.
            var totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize
            );

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Get only the members for the current page.
            var pagedMembers = members
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            // =========================================================
            // EXISTING ROLE DICTIONARY
            // =========================================================

            // Optional: build dictionary of roles if you want to display them
            var memberRoles = new Dictionary<string, string>();

            foreach (var member in pagedMembers)
            {
                var roles = await _userManager.GetRolesAsync(member);
                memberRoles[member.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            ViewBag.MemberRoles = memberRoles;


            // =========================================================
            // PAGINATION VIEWMODEL
            // =========================================================

            var model = new PaginationViewModel<ApplicationUser>
            {
                Items = pagedMembers,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };


            return View(model);
        }


        // GET: Members/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (member == null)
            {
                return NotFound();
            }

            // Add this block to populate the role
            var roles = await _userManager.GetRolesAsync(member);
            ViewBag.MemberRoles = new Dictionary<string, string>
    {
        { member.Id, roles.FirstOrDefault() ?? "No Role" }
    };

            return View(member);
        }


        // GET: Members/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Members/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);


            // Identity already checks if the email is already taken
           


            var member = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                ContactNumber = model.ContactNumber,
                Status = UserStatus.Active,

                // Member-only fields
                Province = model.Province,
                City = model.City,
                Barangay = model.Barangay,
                StreetAddress = model.StreetAddress,
                Gender = model.Gender,
                Birthdate = model.Birthdate
            };

            member.IdPhotoPath = await IdPhotoUploadHelper.SaveIdPhotoAsync(model.IdPhoto);

            // Create user with password
            var result = await _userManager.CreateAsync(member, model.Password);

            if (result.Succeeded)
            {
                // Always assign Member role here
                await _userManager.AddToRoleAsync(member, "Member");
                return RedirectToAction(nameof(Index));
            }

            // Handle errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }


        // GET: Members/Edit/5
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

        // POST: Members/Edit/5
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
                var member = await _userManager.FindByIdAsync(id);
                if (member == null)
                {
                    return NotFound();
                }

                // Update only allowed fields for Members
                member.UserName = model.UserName;
                member.Email = model.Email;
                member.FirstName = model.FirstName;
                member.LastName = model.LastName;
                member.ContactNumber = model.ContactNumber;
                member.Province = model.Province;
                member.City = model.City;
                member.Barangay = model.Barangay;
                member.StreetAddress = model.StreetAddress;
                member.Gender = model.Gender;
                member.Birthdate = model.Birthdate;
                member.Status = model.Status;

                var result = await _userManager.UpdateAsync(member);

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


        // GET: Members/Delete/5
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


        // POST: Members/Delete/5
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

            // Get IDs of the member's listings
            var listingIds = await _context.Listings
                .Where(l => l.MemberId == id)
                .Select(l => l.ListingId)
                .ToListAsync();

            // Get IDs of the member's Lost & Found posts
            var lostFoundIds = await _context.LostFounds
                .Where(l => l.UserId == id)
                .Select(l => l.LostFoundId)
                .ToListAsync();


            // ==========================================
            // DELETE REPORTS
            // ==========================================
            // Delete:
            // 1. Reports submitted by the member
            // 2. Reports targeting the member's Listings
            // 3. Reports targeting the member's Lost & Found posts

            var reports = await _context.UserReports
                .Where(r =>
                    r.ReporterId == id ||
                    (r.ListingId.HasValue &&
                     listingIds.Contains(r.ListingId.Value)) ||
                    (r.LostFoundId.HasValue &&
                     lostFoundIds.Contains(r.LostFoundId.Value)))
                .ToListAsync();

            _context.UserReports.RemoveRange(reports);


            // ==========================================
            // DELETE NOTIFICATIONS
            // ==========================================
            // Delete:
            // 1. Notifications belonging to the member
            // 2. Notifications related to the member's Listings
            // 3. Notifications related to the member's Lost & Found posts

            var notifications = await _context.Notifications
                .Where(n =>
                    n.UserId == id ||
                    (n.ListingId.HasValue &&
                     listingIds.Contains(n.ListingId.Value)) ||
                    (n.LostFoundId.HasValue &&
                     lostFoundIds.Contains(n.LostFoundId.Value)))
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);




            // ==========================================
            // DELETE THE MEMBER
            // ==========================================

            _context.Users.Remove(applicationUser);


            // ==========================================
            // SAVE EVERYTHING  
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
