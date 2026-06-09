using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Index()
        {
            // Only get users in the Member role
            var members = await _userManager.GetUsersInRoleAsync("Member");

            // Optional: build dictionary of roles if you want to display them
            var memberRoles = new Dictionary<string, string>();
            foreach (var member in members)
            {
                var roles = await _userManager.GetRolesAsync(member);
                memberRoles[member.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            ViewBag.MemberRoles = memberRoles;
            return View(members); // pass the list of ApplicationUser objects
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
            if (ModelState.IsValid)
            {
                var member = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    ContactNumber = model.ContactNumber,
                    Status = model.Status ?? "Active",

                    // Member-only fields
                    Address = model.Address,
                    Gender = model.Gender,
                    Birthdate = model.Birthdate
                };

                // Create user with password
                var result = await _userManager.CreateAsync(member, model.Password);

                if (result.Succeeded)
                {
                    // 👇 Always assign Member role here
                    await _userManager.AddToRoleAsync(member, "Member");

                    return RedirectToAction(nameof(Index));
                }

                // Handle errors
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
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
                member.Address = model.Address;
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
            var applicationUser = await _context.Users.FindAsync(id);
            if (applicationUser != null)
            {
                _context.Users.Remove(applicationUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationUserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
