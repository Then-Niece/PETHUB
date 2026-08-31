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

        // Provides centralized audit logging for authentication activities.
        // This records successful login and logout events in the AuditLogs table.
        private readonly AuditLogService _auditLogService;

        public UserAccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, EmailSender emailSender, IConfiguration config, AuditLogService auditLogService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _config = config;
            _auditLogService = auditLogService;
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ensure Terms are accepted
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError(
                    "AcceptTerms",
                    "You must accept the Terms and Conditions.");

                return View(model);
            }

            // Identity already checks if the email is already taken




            var user = new ApplicationUser
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
                Status = UserStatus.Active,
                AcceptedTermsDate = DateTime.UtcNow // 
            };

            user.IdPhotoPath = await IdPhotoUploadHelper.SaveIdPhotoAsync(model.IdPhoto);


            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member"); // assign role

                // Generate the built-in ASP.NET Identity email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Create the confirmation link
                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "UserAccount",
                    new
                    {
                        userId = user.Id,
                        token = token
                    },
                    Request.Scheme);

                // Build email
                var body = EmailTemplateHelper.EmailVerification(
                    user.FirstName,
                    confirmationLink);

                // Send email
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Verify Your PETHUB Account",
                    body);

                // Don't automatically log the user in
                return RedirectToAction("EmailConfirmationSent");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        //terms and conditions get method
        public IActionResult Terms()
        {
            return View();
        }

        // ======================================================
        // EMAIL CONFIRMATION
        // ======================================================


        // GET for Email Confirmation Sent
        [AllowAnonymous]
        public IActionResult EmailConfirmationSent()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Validate the built-in Identity email confirmation token
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                return RedirectToAction("EmailConfirmed");
            }

            // Token is invalid or expired
            return RedirectToAction("EmailConfirmationExpired");
        }


        // GET for Email Confirmed
        [AllowAnonymous]
        public IActionResult EmailConfirmed()
        {
            return View();
        }

        // GET for Email Confirmation Expired
        [AllowAnonymous]
        public IActionResult EmailConfirmationExpired()
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

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
                // Records the successful login event after Identity confirms
                // that the supplied username/email and password are correct.
                // The existing user object already contains the user's Identity ID.
                await _auditLogService.LogAsync(
                    user,
                    "Logged In"
                );

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
            // Check if the user is not allowed to sign in (e.g., email not confirmed)
            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    "",
                    "Please verify your email address before logging in.");

                return View(model);
            }


            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Retrieves the currently authenticated user before the Identity
            // authentication session is cleared by SignOutAsync().
            var user = await _userManager.GetUserAsync(User);

            // Only create the audit record if an authenticated user was found.
            if (user != null)
            {
                // Records the logout event before signing the user out.
                // The service stores the user's ID, role, action, and UTC timestamp.
                await _auditLogService.LogAsync(
                    user,
                    "Logged Out"
                );
            }

            // Clears the user's authentication session.
            await _signInManager.SignOutAsync();

            // Keeps the existing redirect behavior after logout.
            return RedirectToAction("Index", "Home");
        }


        // ======================================================
        // FORGOT PASSWORD AND RESET PASSWORD
        // ======================================================

        //GET for Forgot The Password
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST for Forgot The Password
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);


            if (user == null)
            {
                return View("ForgotPasswordConfirmation");
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Generate the reset password link
            var resetLink = Url.Action(
               "ResetPassword",
                "UserAccount",
                new
                {
                    email = model.Email,
                    token
                },
                Request.Scheme);

            // Build email
            var body = EmailTemplateHelper.PasswordReset(
                user.FirstName,
                resetLink);

            // Send email
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

        [AllowAnonymous]
        public IActionResult ResetPasswordExpired()
        {
            return View();
        }

        //GET for Reset Password
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Get the password reset token provider configured in Identity
            var provider = _userManager.Options.Tokens.PasswordResetTokenProvider;

            // Check if the token is still valid
            var isValidToken = await _userManager.VerifyUserTokenAsync(
                user,
                provider,
                "ResetPassword",
                token
            );

            // If the token is invalid or expired, redirect to the ResetPasswordExpired view
            if (!isValidToken)
            {
                return RedirectToAction("ResetPasswordExpired");
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
            {
                return View(model);
            }

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

            // Token expired or invalid
            if (result.Errors.Any(e =>
                e.Code == "InvalidToken" ||
                e.Description.Contains("invalid", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction("ResetPasswordExpired");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }


            return View(model);
        }



        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AdminSetup(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid administrator invitation.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin || user.Status != UserStatus.Pending)
            {
                return BadRequest(
                    "This administrator invitation is no longer valid.");
            }

            var tokenValid = await _userManager.VerifyUserTokenAsync(
                user,
                "PETHubAdminInvitation",
                "AdminInvitation",
                token);


            if (!tokenValid)
            {
                return BadRequest(
                    "This administrator invitation is invalid or has expired.");
            }

            var model = new AdminViewModel
            {
                Email = user.Email,
                Status = UserStatus.Pending
            };

            ViewBag.UserId = userId;
            ViewBag.Token = token;

            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminSetup(AdminViewModel model, string userId, string token)
        {
            // -----------------------------------------
            // CHECK INVITATION PARAMETERS
            // -----------------------------------------

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid administrator invitation.");
            }

            // -----------------------------------------
            // FIND THE PENDING USER
            // -----------------------------------------

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // -----------------------------------------
            // MAKE SURE THIS IS A PENDING ADMIN
            // -----------------------------------------

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin || user.Status != UserStatus.Pending)
            {
                return BadRequest(
                    "This administrator invitation is no longer valid.");
            }

            // -----------------------------------------
            // VALIDATE INVITATION TOKEN
            // -----------------------------------------

            var tokenValid = await _userManager.VerifyUserTokenAsync(
                user,
                "PETHubAdminInvitation",
                "AdminInvitation",
                token);

            if (!tokenValid)
            {
                return BadRequest(
                    "This administrator invitation is invalid or has expired.");
            }

            // -----------------------------------------
            // CHECK USERNAME AVAILABILITY
            // -----------------------------------------

            var existingUser = await _userManager.FindByNameAsync(model.UserName);

            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    "UserName",
                    "This username is already taken.");

                model.Email = user.Email;

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View(model);
            }

            // -----------------------------------------
            // VALIDATE FORM
            // -----------------------------------------

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View(model);
            }

            // -----------------------------------------
            // UPDATE PERSONAL INFORMATION
            // -----------------------------------------

            user.UserName = model.UserName;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.ContactNumber = model.ContactNumber;

            // -----------------------------------------
            // SET PASSWORD
            // -----------------------------------------

            var passwordResult = await _userManager.AddPasswordAsync(
                user,
                model.Password);


            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                model.Email = user.Email;

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View(model);
            }

            // -----------------------------------------
            // ACTIVATE ACCOUNT
            // -----------------------------------------

            user.EmailConfirmed = true;
            user.Status = UserStatus.Active;

            // -----------------------------------------
            // SAVE USER
            // -----------------------------------------

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                model.Email = user.Email;

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View(model);
            }

            // -----------------------------------------
            // SUCCESS
            // -----------------------------------------

            return RedirectToAction("Login");
        }



    }

}

