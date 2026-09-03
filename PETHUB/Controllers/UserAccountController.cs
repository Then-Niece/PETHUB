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
        // =========================================================
        // DEPENDENCIES
        // =========================================================

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

        // =========================================================
        // REGISTER
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            ViewData["HideSidebar"] = true;

            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            ViewData["HideSidebar"] = true;


            // =====================================================
            // VALIDATE FORM
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            if (!model.AcceptTerms)
            {
                ModelState.AddModelError(
                    nameof(model.AcceptTerms),
                    "You must accept the Terms and Conditions."
                );

                return View(model);
            }


            // =====================================================
            // CREATE USER OBJECT
            // =====================================================

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

                Status = UserStatus.Pending,

                AcceptedTermsDate = DateTime.UtcNow
            };


            // =====================================================
            // CREATE IDENTITY ACCOUNT FIRST
            // =====================================================

            var result =
                await _userManager.CreateAsync(
                    user,
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
                    user,
                    "Member"
                );


            if (!roleResult.Succeeded)
            {
                // Account creation was incomplete.
                // Remove the newly created account.
                await _userManager.DeleteAsync(user);

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
            // SAVE ID PHOTO
            // =====================================================

            if (model.IdPhoto != null)
            {
                user.IdPhotoPath =
                    await IdPhotoUploadHelper
                        .SaveIdPhotoAsync(model.IdPhoto);


                var updateResult =
                    await _userManager.UpdateAsync(user);


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
            // GENERATE EMAIL CONFIRMATION TOKEN
            // =====================================================

            var token =
                await _userManager
                    .GenerateEmailConfirmationTokenAsync(user);


            var confirmationLink =
                Url.Action(
                    nameof(ConfirmEmail),
                    "UserAccount",
                    new
                    {
                        userId = user.Id,
                        token
                    },
                    Request.Scheme
                );


            // =====================================================
            // SEND VERIFICATION EMAIL
            // =====================================================

            var body =
                EmailTemplateHelper.EmailVerification(
                    user.FirstName,
                    confirmationLink!
                );


            await _emailSender.SendEmailAsync(
                user.Email!,
                "Verify Your PETHUB Account",
                body
            );


            return RedirectToAction(
                nameof(EmailConfirmationSent)
            );
        }


        // =========================================================
        // TERMS AND CONDITIONS
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Terms()
        {
            return View();
        }


        // =========================================================
        // EMAIL CONFIRMATION
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult EmailConfirmationSent()
        {
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            var user =
                await _userManager.FindByIdAsync(userId);


            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }



            // =====================================================
            // EMAIL ALREADY VERIFIED
            // =====================================================

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            // =====================================================
            // VERIFY EMAIL
            // =====================================================

            var result =
                await _userManager.ConfirmEmailAsync(
                    user,
                    token
                );


            if (!result.Succeeded)
            {
                return RedirectToAction(
                    nameof(EmailConfirmationExpired)
                );
            }


            // =====================================================
            // ACTIVATE VERIFIED ACCOUNT
            // =====================================================

            user.Status = UserStatus.Active;


            var updateResult =
                await _userManager.UpdateAsync(user);


            if (!updateResult.Succeeded)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            return RedirectToAction(
                nameof(EmailConfirmed)
            );
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult EmailConfirmed()
        {
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult EmailConfirmationExpired()
        {
            return View();
        }



        // =========================================================
        // RESEND EMAIL CONFIRMATION - GET
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }


        // =========================================================
        // RESEND EMAIL CONFIRMATION - POST
        // =========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user =
                await _userManager.FindByEmailAsync(
                    model.Email
                );


            // Do not reveal whether the email exists.
            if (user == null)
            {
                return View(
                    "ResendEmailConfirmationSent"
                );
            }


            // Already verified.
            if (user.EmailConfirmed)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            // =====================================================
            // GENERATE NEW EMAIL CONFIRMATION TOKEN
            // =====================================================

            var token =
                await _userManager
                    .GenerateEmailConfirmationTokenAsync(user);


            var confirmationLink =
                Url.Action(
                    nameof(ConfirmEmail),
                    "UserAccount",
                    new
                    {
                        userId = user.Id,
                        token
                    },
                    Request.Scheme
                );


            // =====================================================
            // SEND VERIFICATION EMAIL
            // =====================================================

            var body =
                EmailTemplateHelper.EmailVerification(
                    user.FirstName,
                    confirmationLink!
                );


            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Verify Your PETHUB Account",
                    body
                );
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to send the verification email right now. Please try again later."
                );

                return View(model);
            }


            return View(
                "ResendEmailConfirmationSent"
            );
        }



        // =========================================================
        // LOGIN
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(bool deactivated = false)
        {
            ViewData["HideSidebar"] = true;

            if (deactivated)
            {
                TempData["WarningMessage"] = "Your account has been deactivated.\n Please contact an administrator if you believe this was a mistake.";
            }

            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["HideSidebar"] = true;


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // Allow username OR email.
            var user =
                await _userManager.FindByNameAsync(
                    model.UserNameOrEmail
                )
                ??
                await _userManager.FindByEmailAsync(
                    model.UserNameOrEmail
                );


            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid login attempt."
                );

                return View(model);
            }

            // Checks if the user is active or inactive
            if (user.Status == UserStatus.Inactive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This account has been deactivated. Please contact the PetHub administrator for assistance."
                );

                return View(model);
            }


            var result =
                await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true
                );


            // =====================================================
            // SUCCESS
            // =====================================================

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(
                    user,
                    "Admin"))
                {
                    return RedirectToAction(
                        "Index",
                        "AdminDashboard"
                    );
                }


                if (await _userManager.IsInRoleAsync(
                    user,
                    "Member"))
                {
                    return RedirectToAction(
                        "Feed",
                        "PetFeeds"
                    );
                }


                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }


            // =====================================================
            // LOCKED ACCOUNT
            // =====================================================

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account has been temporarily locked because of too many failed login attempts. Please try again in 15 minutes."
                );

                return View(model);
            }


            // =====================================================
            // EMAIL NOT CONFIRMED
            // =====================================================

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please verify your email address before logging in."
                );

                return View(model);
            }


            // =====================================================
            // INVALID PASSWORD
            // =====================================================

            var failedAttempts =
                await _userManager
                    .GetAccessFailedCountAsync(user);


            var maxFailedAttempts =
                _userManager.Options
                    .Lockout
                    .MaxFailedAccessAttempts;


            var remainingAttempts =
                Math.Max(
                    maxFailedAttempts - failedAttempts,
                    0
                );


            var attemptText =
                remainingAttempts == 1
                    ? "attempt"
                    : "attempts";


            ModelState.AddModelError(
                string.Empty,
                $"Invalid login attempt. {remainingAttempts} {attemptText} remaining before your account is temporarily locked."
            );


            return View(model);
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index",
                "Home"
            );
        }


        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user =
                await _userManager.FindByEmailAsync(
                    model.Email
                );


            // Do not reveal whether the email exists.
            if (user == null)
            {
                return View(
                    "ForgotPasswordConfirmation"
                );
            }


            // =====================================================
            // GENERATE RESET TOKEN
            // =====================================================

            var token =
                await _userManager
                    .GeneratePasswordResetTokenAsync(user);


            var resetLink =
                Url.Action(
                    nameof(ResetPassword),
                    "UserAccount",
                    new
                    {
                        email = model.Email,
                        token
                    },
                    Request.Scheme
                );


            // =====================================================
            // SEND RESET EMAIL
            // =====================================================

            var body =
                EmailTemplateHelper.PasswordReset(
                    user.FirstName,
                    resetLink!
                );


            await _emailSender.SendEmailAsync(
                user.Email!,
                "Reset Password",
                body
            );


            return View(
                "ForgotPasswordConfirmation"
            );
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        // =========================================================
        // RESET PASSWORD EXPIRED
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordExpired()
        {
            return View();
        }


        // =========================================================
        // RESET PASSWORD - GET
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(
            string token,
            string email)
        {
            if (string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            var user =
                await _userManager.FindByEmailAsync(email);


            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            var provider =
                _userManager.Options
                    .Tokens
                    .PasswordResetTokenProvider;


            var isValidToken =
                await _userManager.VerifyUserTokenAsync(
                    user,
                    provider,
                    "ResetPassword",
                    token
                );


            if (!isValidToken)
            {
                return RedirectToAction(
                    nameof(ResetPasswordExpired)
                );
            }


            return View(
                new ResetPasswordViewModel
                {
                    Token = token,
                    Email = email
                }
            );
        }


        // =========================================================
        // RESET PASSWORD - POST
        // =========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user =
                await _userManager.FindByEmailAsync(
                    model.Email
                );


            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login)
                );
            }


            // Prevent using the current password again
            var isSamePassword =
                await _userManager.CheckPasswordAsync(
                    user,
                    model.Password
                );

            if (isSamePassword)
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    "Your new password cannot be the same as your current password."
                );

                return View(model);
            }



            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password
                );


            // =====================================================
            // SUCCESS
            // =====================================================

            if (result.Succeeded)
            {
                var loginLink =
                    Url.Action(
                        nameof(Login),
                        "UserAccount",
                        null,
                        Request.Scheme
                    );


                // Password reset has already succeeded.
                // Failure to send the notification should not
                // undo or block the reset.
                try
                {
                    var emailBody =
                        EmailTemplateHelper.PasswordChanged(
                            user.FirstName,
                            loginLink!
                        );


                    await _emailSender.SendEmailAsync(
                        user.Email!,
                        "Your PETHUB Password Was Changed",
                        emailBody
                    );
                }
                catch (Exception)
                {
                    // Later, we can log this failure.
                }


                return RedirectToAction(
                    nameof(ResetPasswordConfirmation)
                );
            }


            // =====================================================
            // INVALID / EXPIRED TOKEN
            // =====================================================

            if (result.Errors.Any(
                error =>
                    error.Code == "InvalidToken"))
            {
                return RedirectToAction(
                    nameof(ResetPasswordExpired)
                );
            }


            // =====================================================
            // PASSWORD VALIDATION ERRORS
            // =====================================================

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description
                );
            }


            return View(model);
        }


        // =========================================================
        // RESET PASSWORD CONFIRMATION
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        // =========================================================
        // ADMIN INVITATION SETUP - GET
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AdminSetup(
            string userId,
            string token)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(
                    "Invalid administrator invitation."
                );
            }


            var user =
                await _userManager.FindByIdAsync(userId);


            if (user == null)
            {
                return NotFound();
            }


            var isAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin"
                );


            if (!isAdmin ||
                user.Status != UserStatus.Pending)
            {
                return BadRequest(
                    "This administrator invitation is no longer valid."
                );
            }


            var tokenValid =
                await _userManager.VerifyUserTokenAsync(
                    user,
                    "PETHubAdminInvitation",
                    "AdminInvitation",
                    token
                );


            if (!tokenValid)
            {
                return BadRequest(
                    "This administrator invitation is invalid or has expired."
                );
            }


            var model =
                new AdminViewModel
                {
                    Email = user.Email,
                    Status = UserStatus.Pending
                };


            ViewBag.UserId = userId;
            ViewBag.Token = token;


            return View(model);
        }


        // =========================================================
        // ADMIN INVITATION SETUP - POST
        // =========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminSetup(
            AdminViewModel model,
            string userId,
            string token)
        {
            // =====================================================
            // VALIDATE INVITATION
            // =====================================================

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(
                    "Invalid administrator invitation."
                );
            }


            var user =
                await _userManager.FindByIdAsync(userId);


            if (user == null)
            {
                return NotFound();
            }


            var isAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin"
                );


            if (!isAdmin ||
                user.Status != UserStatus.Pending)
            {
                return BadRequest(
                    "This administrator invitation is no longer valid."
                );
            }


            var tokenValid =
                await _userManager.VerifyUserTokenAsync(
                    user,
                    "PETHubAdminInvitation",
                    "AdminInvitation",
                    token
                );


            if (!tokenValid)
            {
                return BadRequest(
                    "This administrator invitation is invalid or has expired."
                );
            }


            // =====================================================
            // USERNAME AVAILABILITY
            // =====================================================

            var existingUser =
                await _userManager.FindByNameAsync(
                    model.UserName
                );


            if (existingUser != null &&
                existingUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(model.UserName),
                    "This username is already taken."
                );
            }


            // =====================================================
            // FORM VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View(model);
            }


            // =====================================================
            // UPDATE ADMIN INFORMATION
            // =====================================================

            user.UserName = model.UserName;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.ContactNumber = model.ContactNumber;


            var profileResult =
                await _userManager.UpdateAsync(user);


            if (!profileResult.Succeeded)
            {
                foreach (var error in profileResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }


                model.Email = user.Email;

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View(model);
            }


            // =====================================================
            // SET ADMIN PASSWORD
            // =====================================================

            var passwordResult =
                await _userManager.AddPasswordAsync(
                    user,
                    model.Password
                );


            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }


                model.Email = user.Email;

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View(model);
            }


            // =====================================================
            // ACTIVATE ADMIN ACCOUNT
            // =====================================================

            user.EmailConfirmed = true;
            user.Status = UserStatus.Active;


            var activationResult =
                await _userManager.UpdateAsync(user);


            if (!activationResult.Succeeded)
            {
                /*
                 * Try to undo the password addition so the
                 * invitation can still be attempted again.
                 */
                await _userManager.RemovePasswordAsync(user);


                foreach (var error in activationResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }


                model.Email = user.Email;

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View(model);
            }


            return RedirectToAction(
                nameof(AdminSetupConfirmation)
            );
        }


        // =========================================================
        // ADMIN SETUP CONFIRMATION
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AdminSetupConfirmation()
        {
            return View();
        }


    }
}