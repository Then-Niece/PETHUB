using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    // Only authenticated users can access this controller.
    [Authorize]
    public class MyProfileController : Controller
    {
        // ==========================================================
        // DEPENDENCIES
        // ==========================================================

        // Provides access to the currently logged-in Identity user.
        private readonly UserManager<ApplicationUser> _userManager;

        // Provides access to the web hosting environment.
        // This is needed by ProfilePictureHelper when managing files.
        private readonly IWebHostEnvironment _environment;

        // Provides Member profile-building logic.
        private readonly IProfileService _profileService;

        // Provides centralized audit logging for Member activities.
        // The service saves the event to the AuditLogs table.
        private readonly AuditLogService _auditLogService;

        // ==========================================================
        // CONSTRUCTOR
        // ==========================================================

        public MyProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, IProfileService profileService, AuditLogService auditLogService)
        {
            _userManager = userManager;
            _environment = environment;
            _profileService = profileService;
            _auditLogService = auditLogService;
        }

        // ==========================================================
        // VIEW PROFILE
        // ==========================================================

        // Displays the currently logged-in Member's profile.
        [HttpGet]
        public async Task<IActionResult> View()
        {
            // Retrieve the currently logged-in user.
            var user = await _userManager.GetUserAsync(User);

            // Make sure the user exists.
            if (user == null)
            {
                return Unauthorized();
            }

            // Build and display the profile ViewModel.
            return View(await _profileService.BuildProfileViewModelAsync(user));
        }


        // ==========================================================
        // EDIT PROFILE - GET
        // ==========================================================

        // Displays the Edit Profile page.
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Retrieve the currently logged-in user.
            var user = await _userManager.GetUserAsync(User);

            // Make sure the user exists.
            if (user == null)
            {
                return Unauthorized();
            }

            // Build the Edit Profile ViewModel.
            return View(await _profileService.BuildProfileViewModelAsync(user));
        }


        // ==========================================================
        // EDIT PROFILE - POST
        // ==========================================================

        // Handles the submitted Edit Profile form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            // ======================================================
            // MODEL VALIDATION
            // ======================================================

            // Check whether the submitted model passed validation.
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ======================================================
            // GET CURRENT USER
            // ======================================================

            // Retrieve the currently logged-in Member.
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }


            // ======================================================
            // CURRENT FILE PATHS
            // ======================================================

            // Start by keeping the current profile picture.
            //
            // If the user does nothing with the profile picture,
            // this value will remain unchanged.
            string? newProfilePicturePath = user.ProfilePicturePath;


            // Start by keeping the current Valid ID.
            //
            // If the user does not upload a new Valid ID,
            // this value will remain unchanged.
            string? newIdPhotoPath = user.IdPhotoPath;


            // ======================================================
            // PROFILE PICTURE
            // ======================================================


            // ------------------------------------------------------
            // CASE 1:
            // User clicked "Remove Profile Picture"
            // ------------------------------------------------------

            if (model.RemoveProfilePicture)
            {
                // Delete the existing physical profile picture.
                ProfilePictureHelper.DeleteProfilePicture(
                    user.ProfilePicturePath,
                    _environment.WebRootPath
                );

                // Remove the profile picture path from the database.
                newProfilePicturePath = null;
            }


            // ------------------------------------------------------
            // CASE 2:
            // User selected a NEW profile picture
            // ------------------------------------------------------

            else if (model.ProfilePictureFile != null)
            {
                // Validate the selected profile picture.
                string? validationError =
                    ProfilePictureHelper.ValidateProfilePicture(
                        model.ProfilePictureFile
                    );


                // Stop if validation failed.
                if (validationError != null)
                {
                    ModelState.AddModelError(
                        nameof(model.ProfilePictureFile),
                        validationError
                    );

                    return View(model);
                }


                // Save the NEW profile picture first.
                //
                // This is safer because we don't want to delete
                // the old picture before knowing that the new
                // picture was successfully saved.
                newProfilePicturePath =
                    await ProfilePictureHelper.SaveProfilePictureAsync(
                        model.ProfilePictureFile,
                        _environment.WebRootPath
                    );


                // Delete the OLD profile picture after the
                // new picture has been successfully saved.
                ProfilePictureHelper.DeleteProfilePicture(
                    user.ProfilePicturePath,
                    _environment.WebRootPath
                );
            }


            // ======================================================
            // MEMBER VALID ID
            // ======================================================

            // Only process the Valid ID if the user selected one.
            if (model.IdPhotoFile != null)
            {
                // --------------------------------------------------
                // Validate file extension
                // --------------------------------------------------

                string[] allowedExtensions =
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };


                // Get the extension of the uploaded file.
                string extension =
                    Path.GetExtension(
                        model.IdPhotoFile.FileName
                    ).ToLowerInvariant();


                // Reject unsupported file types.
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.IdPhotoFile),
                        "Only JPG, JPEG, PNG, and WEBP files are allowed."
                    );

                    return View(model);
                }


                // --------------------------------------------------
                // Validate file size
                // --------------------------------------------------

                // Maximum Valid ID size: 5 MB.
                const long maxFileSize =
                    5 * 1024 * 1024;


                // Reject files larger than 5 MB.
                if (model.IdPhotoFile.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        nameof(model.IdPhotoFile),
                        "Valid ID cannot exceed 5 MB."
                    );

                    return View(model);
                }


                // --------------------------------------------------
                // Save the NEW Valid ID
                // --------------------------------------------------

                // Save the new ID using the helper.
                string? savedIdPhotoPath =
                    await IdPhotoUploadHelper.SaveIdPhotoAsync(
                        model.IdPhotoFile
                    );


                // Make sure the helper actually returned a path.
                if (string.IsNullOrEmpty(savedIdPhotoPath))
                {
                    ModelState.AddModelError(
                        nameof(model.IdPhotoFile),
                        "The Valid ID could not be uploaded."
                    );

                    return View(model);
                }

                // Store the new path.
                newIdPhotoPath = savedIdPhotoPath;

                // --------------------------------------------------
                // Delete the OLD Valid ID
                // --------------------------------------------------

                if (!string.IsNullOrEmpty(user.IdPhotoPath) &&
                    user.IdPhotoPath.StartsWith(
                        "/uploads/memberids/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Extract only the filename.
                    string oldFileName =
                        Path.GetFileName(
                            user.IdPhotoPath
                        );


                    // Build the old physical file path.
                    string oldPhysicalPath =
                        Path.Combine(
                            _environment.WebRootPath,
                            "uploads",
                            "memberids",
                            oldFileName
                        );


                    // Delete the old file if it exists.
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }
            }


            // ======================================================
            // UPDATE MEMBER INFORMATION
            // ======================================================

            user.FirstName = model.FirstName;
            user.MiddleName = model.MiddleName;
            user.LastName = model.LastName;
            user.ContactNumber = model.ContactNumber;

            // Member-specific information.
            user.Gender = model.Gender;
            user.Birthdate = model.Birthdate;

            // Member address.
            user.Province = model.Province;
            user.Barangay = model.Barangay;
            user.StreetAddress = model.StreetAddress;

            // Member biography.
            user.Bio = model.Bio;

            // ======================================================
            // UPDATE FILE PATHS
            // ======================================================

            // Save the profile picture path.
            user.ProfilePicturePath = newProfilePicturePath;

            // Save the Valid ID path.
            user.IdPhotoPath = newIdPhotoPath;


            // ======================================================
            // SAVE THROUGH ASP.NET IDENTITY
            // ======================================================

            // Saves the updated Member profile through ASP.NET Identity.
            // UpdateAsync returns a result indicating whether the database update succeeded.
            var result = await _userManager.UpdateAsync(user);

            // Check whether Identity successfully saved the user.
            // If the update failed, the audit log is NOT created because the profile
            // change did not actually succeed.
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    // Adds each Identity error back to ModelState so the Razor View
                    // can display the reason why the update failed.
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                // Returns the user to the edit form instead of recording a false
                // "Profile Updated" event.
                return View(model);
            }

            // Records the successful profile update in the AuditLogs table.
            // LogAsync determines the user's current role and stores the exact UTC time.
            await _auditLogService.LogAsync(
                user,
                "Profile Updated"
            );

            // Stores a one-time success message that can be displayed after redirect.
            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            // Redirects back to the Member's profile page after a successful update.
            return RedirectToAction(
                nameof(View)
            );
        }
    }
}