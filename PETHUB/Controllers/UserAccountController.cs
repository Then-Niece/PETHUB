using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;


namespace PETHUB.Controllers
{
    public class UserAccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailSender _emailSender;
        //gamiton rani sya ug IConfiguration para sa pagkuha sa appsettings.json values
        private readonly IConfiguration _config;

        public UserAccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, EmailSender emailSender, IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _config = config;
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["HideSidebar"] = true; // Hide the sidebar for the registration page
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Ensure Terms are accepted
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("AcceptTerms", "You must accept the Terms and Conditions.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                ContactNumber = model.ContactNumber,
                Address = model.Address,
                Gender = model.Gender,
                Birthdate = model.Birthdate,
                Status = UserStatus.Active,
                AcceptedTermsDate = DateTime.UtcNow // 
            };

            user.IdPhotoPath = await IdPhotoUploadHelper.SaveIdPhotoAsync(model.IdPhoto);


            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member"); // assign role
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        //terms and conditions get method
        public IActionResult Terms()
        {
            return View();
        }


        [HttpGet]
        public IActionResult Login()
        {
            ViewData["HideSidebar"] = true; // Hide the sidebar for the login page
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Allow login by either username or email
            var user = await _userManager.FindByNameAsync(model.UserNameOrEmail)
                       ?? await _userManager.FindByEmailAsync(model.UserNameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "PetFeeds");

                }
                else if (await _userManager.IsInRoleAsync(user, "Member"))
                {
                    return RedirectToAction("Feed", "PetFeeds");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }


            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        //GET for Forgot The Password
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);


            if (user == null)
            {
                return View("ForgotPasswordConfirmation");
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action(
               "ResetPassword",
                "UserAccount",
                new
                {
                    email = model.Email,
                    token
                },
                Request.Scheme);



            var body = EmailTemplateHelper.PasswordReset(
                user.FirstName,
                resetLink);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset Password",
                body);


            return View("ForgotPasswordConfirmation");
        }

        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //GET for Reset Password
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            });
        }

        //POST for Reset Password
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);


            var user = await _userManager.FindByEmailAsync(model.Email);


            if (user == null)
            {
                return RedirectToAction("Login");
            }


            var result = await _userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.Password);


            if (result.Succeeded)
            {
                return RedirectToAction("Login");
            }


            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }


            return View(model);
        }
    }
}
