using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;
using PETHUB.Helpers;

namespace PETHUB.Controllers
{
    // Only authenticated Admin users can access this controller.
    [Authorize(Roles = "Admin")]
    public class AdminMyProfileController : Controller
    {
        // Provides access to the currently logged-in Identity user.
        private readonly UserManager<ApplicationUser> _userManager;

        // Provides access to the web hosting environment.
        private readonly IWebHostEnvironment _environment;

        // Provides Admin profile-building logic.
        private readonly AdminIProfileService _profileService;


        // Receives required services through Dependency Injection.
        public AdminMyProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, AdminIProfileService profileService)
        {
            _userManager = userManager;
            _environment = environment;
            _profileService = profileService;
        }


        // ==========================================================
        // VIEW PROFILE
        // ==========================================================

        // Displays the currently logged-in Admin's profile.
        [HttpGet]
        public async Task<IActionResult> View()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            return View(await _profileService.BuildAdminProfileViewModelAsync(user)
            );
        }


        // ==========================================================
        // EDIT PROFILE - GET
        // ==========================================================

        // Displays the Edit Profile page.
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            return View(await _profileService.BuildAdminProfileViewModelAsync(user)
            );
        }


        // ==========================================================
        // EDIT PROFILE - POST
        // ==========================================================

        // Handles the submitted Edit Profile form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminEditProfileViewModel model)
        {
            // Validate the submitted model.
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // Retrieve the currently logged-in Admin.
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }


            // ======================================================
            // PROFILE PICTURE
            // ======================================================

            // Start by keeping the current profile picture.
            string? newProfilePicturePath = user.ProfilePicturePath;


            // ------------------------------------------------------
            // CASE 1:
            // User clicked "Remove Photo"
            // ------------------------------------------------------

            if (model.RemoveProfilePicture)
            {
                // Delete the existing physical image.
                ProfilePictureHelper.DeleteProfilePicture(user.ProfilePicturePath, _environment.WebRootPath);

                // Remove the path from the database.
                newProfilePicturePath = null;
            }


            // ------------------------------------------------------
            // CASE 2:
            // User selected a NEW profile picture
            // ------------------------------------------------------

            else if (model.ProfilePictureFile != null)
            {
                // Validate the image.
                string? validationError = ProfilePictureHelper.ValidateProfilePicture(model.ProfilePictureFile);

                // Stop if validation failed.
                if (validationError != null)
                {
                    ModelState.AddModelError(nameof(model.ProfilePictureFile),
                        validationError
                    );

                    return View(model);
                }


                // Save the new profile picture first.
                newProfilePicturePath =
                    await ProfilePictureHelper.SaveProfilePictureAsync(
                        model.ProfilePictureFile,
                        _environment.WebRootPath
                    );

                // Only delete the old picture after the new one
                // has been successfully saved.
                ProfilePictureHelper.DeleteProfilePicture(
                    user.ProfilePicturePath,
                    _environment.WebRootPath
                );
            }


            // ======================================================
            // UPDATE EDITABLE USER INFORMATION
            // ======================================================

            user.FirstName = model.FirstName;

            user.MiddleName = model.MiddleName;

            user.LastName = model.LastName;

            user.ContactNumber = model.ContactNumber;

            user.Bio = model.Bio;


            // Update profile picture path.
            user.ProfilePicturePath = newProfilePicturePath;


            // ======================================================
            // SAVE USER THROUGH ASP.NET IDENTITY
            // ======================================================

            var result = await _userManager.UpdateAsync(user);


            // Check whether Identity successfully saved the user.
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


            // ======================================================
            // SUCCESS
            // ======================================================

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";


            // Return Admin to the profile page.
            return RedirectToAction(nameof(View));
        }
    }
}