using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    public class MembersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailSender _emailSender;

        public MembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;

        }


        // GET: Members
        public async Task<IActionResult> Index()
        {
            // Only get users in the Member role
            var members = (await _userManager.GetUsersInRoleAsync("Member"))
                .OrderByDescending(m => m.CreatedAt) // Newest first
                .ToList();

            // Optional: build dictionary of roles if you want to display them
            var memberRoles = new Dictionary<string, string>();
            foreach (var member in members)
            {
                var roles = await _userManager.GetRolesAsync(member);
                memberRoles[member.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            ViewBag.MemberRoles = memberRoles;
            return View(members);
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

        // =========================================================
        // CREATE MEMBER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // =====================================================
            // CREATE MEMBER OBJECT
            // =====================================================

            var member = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,

                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    ContactNumber = model.ContactNumber,

                    Province = model.Province,
                    City = model.City,
                    Barangay = model.Barangay,
                    StreetAddress = model.StreetAddress,

                    Gender = model.Gender,
                    Birthdate = model.Birthdate,

                    Status = UserStatus.Pending
                };


            // =====================================================
            // CREATE IDENTITY ACCOUNT
            // =====================================================

            var result =
                await _userManager.CreateAsync(
                    member,
                    model.Password
                );


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            // =====================================================
            // ASSIGN MEMBER ROLE
            // =====================================================

            var roleResult =
                await _userManager.AddToRoleAsync(
                    member,
                    "Member"
                );


            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(member);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            // =====================================================
            // SAVE VALID ID
            // =====================================================

            if (model.IdPhoto != null)
            {
                member.IdPhotoPath =
                    await IdPhotoUploadHelper
                        .SaveIdPhotoAsync(model.IdPhoto);


                var updateResult =
                    await _userManager.UpdateAsync(member);


                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description
                        );
                    }

                    return View(model);
                }
            }


            // =====================================================
            // GENERATE EMAIL VERIFICATION TOKEN
            // =====================================================

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(member);


            var confirmationLink =
                Url.Action(
                    "ConfirmEmail",
                    "UserAccount",
                    new
                    {
                        userId = member.Id,
                        token
                    },
                    Request.Scheme
                );


            // =====================================================
            // SEND VERIFICATION EMAIL
            // =====================================================

            try
            {
                var body =
                    EmailTemplateHelper
                        .AdminCreatedMemberVerification(
                            member.FirstName,
                            confirmationLink!
                        );

                await _emailSender.SendEmailAsync(
                    member.Email!,
                    "Your PETHUB Member Account Has Been Created",
                    body
                );

                TempData["SuccessMessage"] =
                    "Member account created. A verification email has been sent.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "The member account was created, but the verification email could not be sent.";
            }

            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // EDIT MEMBER - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }


            var member =
                await _userManager.FindByIdAsync(id);


            if (member == null)
            {
                return NotFound();
            }


            var isMember =
                await _userManager.IsInRoleAsync(
                    member,
                    "Member"
                );


            if (!isMember)
            {
                return NotFound();
            }


            var model =
                new EditMemberViewModel
                {
                    Id = member.Id,

                    UserName = member.UserName!,
                    Email = member.Email!,

                    FirstName = member.FirstName,
                    LastName = member.LastName,
                    ContactNumber = member.ContactNumber,

                    Gender = member.Gender,
                    Birthdate = member.Birthdate,

                    Province = member.Province,
                    City = member.City,
                    Barangay = member.Barangay,
                    StreetAddress = member.StreetAddress,

                    Status = member.Status,
                    IdPhotoPath = member.IdPhotoPath
                };


            return View(model);
        }

        // =========================================================
        // EDIT MEMBER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            EditMemberViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var member =
                await _userManager.FindByIdAsync(id);


            if (member == null)
            {
                return NotFound();
            }


            var isMember =
                await _userManager.IsInRoleAsync(
                    member,
                    "Member"
                );


            if (!isMember)
            {
                return NotFound();
            }


            // =====================================================
            // CHECK USERNAME
            // =====================================================

            var existingUsername =
                await _userManager.FindByNameAsync(
                    model.UserName
                );


            if (existingUsername != null &&
                existingUsername.Id != member.Id)
            {
                ModelState.AddModelError(
                    nameof(model.UserName),
                    "This username is already taken."
                );

                return View(model);
            }

            // =====================================================
            // UPDATE ALLOWED FIELDS ONLY
            // =====================================================

            member.UserName = model.UserName;

            member.FirstName = model.FirstName;
            member.LastName = model.LastName;
            member.ContactNumber = model.ContactNumber;

            member.Gender = model.Gender;
            member.Birthdate = model.Birthdate;

            member.Province = model.Province;
            member.City = model.City;
            member.Barangay = model.Barangay;
            member.StreetAddress = model.StreetAddress;


            // Status is intentionally NOT changed here.


            var result =
                await _userManager.UpdateAsync(member);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            return RedirectToAction(
                nameof(Index)
            );
        }

        // =========================================================
        // DEACTIVATE MEMBER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var member =
                await _userManager.FindByIdAsync(id);

            if (member == null)
            {
                return NotFound();
            }

            var isMember =
                await _userManager.IsInRoleAsync(
                    member,
                    "Member"
                );

            if (!isMember)
            {
                return NotFound();
            }

            if (member.Status == UserStatus.Inactive)
            {
                return RedirectToAction(nameof(Index));
            }

            member.Status = UserStatus.Inactive;

            var result =
                await _userManager.UpdateAsync(member);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Unable to deactivate the member account.";

                return RedirectToAction(nameof(Index));
            }

            await _userManager.UpdateSecurityStampAsync(member);

            TempData["SuccessMessage"] =
                "Member account has been deactivated.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // REACTIVATE MEMBER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }


            var member =
                await _userManager.FindByIdAsync(id);


            if (member == null)
            {
                return NotFound();
            }


            var isMember =
                await _userManager.IsInRoleAsync(
                    member,
                    "Member"
                );


            if (!isMember)
            {
                return NotFound();
            }


            member.Status = UserStatus.Active;


            var result =
                await _userManager.UpdateAsync(member);


            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Unable to reactivate the member.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            TempData["SuccessMessage"] =
                "Member account has been reactivated.";


            return RedirectToAction(
                nameof(Index)
            );
        }
    }
}
