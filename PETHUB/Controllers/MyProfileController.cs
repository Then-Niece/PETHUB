using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{

    // Only authenticated users can access this controller.
    [Authorize]
    public class MyProfileController : Controller
    {

        // Provides access to the currently logged-in Identity user.
        private readonly UserManager<ApplicationUser> _userManager;

        // Provides access to the application's database.
        private readonly ApplicationDbContext _context;

        // Provides access to the web hosting environment, useful for file uploads.
        private readonly IWebHostEnvironment _environment;

        private const string ProfilePictureFolder = "uploads/profilepictures";
        private const string ProfilePictureUrlPrefix = "/uploads/profilepictures/";

        // Receives required services through Dependency Injection.
        // ASP.NET Core automatically provides these services at runtime.
        public MyProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        // Displays the Edit My Profile page for the currently logged-in user.
        // This action retrieves the user's information from the database
        // and maps it into the EditProfileViewModel.
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Retrieves the currently authenticated user.
            // If no user is found, return Unauthorized.
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            // Map the ApplicationUser entity into the ViewModel.
            var model = new EditProfileViewModel
            {
                // Editable fields
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                ContactNumber = user.ContactNumber,
                Gender = user.Gender,
                Birthdate = user.Birthdate,
                Province = user.Province,
                City = user.City,
                Barangay = user.Barangay,
                StreetAddress = user.StreetAddress,
                Bio = user.Bio,

                // Read-only information
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Status = user.Status,
                AcceptedTermsDate = user.AcceptedTermsDate,
                IdPhotoPath = user.IdPhotoPath,
                ProfilePicturePath = user.ProfilePicturePath
            };

            // Send the populated ViewModel to the view.
            return View(model);
        }

        // Handles the submission of the Edit My Profile form.
        // Receives the values entered by the user from the EditProfileViewModel.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            // Check if the submitted model passed validation.
            // If validation fails, redisplay the page with the entered values.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Retrieve the currently logged-in user.
            // This ensures users can only edit their own profile.
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }


            // ==========================================================
            // PROFILE PICTURE UPLOAD
            // ==========================================================

            string? newProfilePicturePath = user.ProfilePicturePath;

            // Check whether the user selected a new profile picture.
            if (model.ProfilePictureFile != null)
            {
                // Allowed image extensions.
                string[] allowedExtensions =
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                // Gets the uploaded file extension.
                string extension = Path.GetExtension(model.ProfilePictureFile.FileName)
                                       .ToLowerInvariant();

                // Reject unsupported image types.
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.ProfilePictureFile),
                        "Only JPG, JPEG, PNG, and WEBP files are allowed.");

                    return View(model);
                }

                // Maximum upload size (5 MB).
                const long maxFileSize = 5 * 1024 * 1024;

                // Reject files larger than the limit.
                if (model.ProfilePictureFile.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        nameof(model.ProfilePictureFile),
                        "Profile picture cannot exceed 5 MB.");

                    return View(model);
                }

                string uploadsFolderPath = Path.Combine(
                    _environment.WebRootPath,
                    ProfilePictureFolder);

                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }

                string uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string physicalFilePath = Path.Combine(uploadsFolderPath, uniqueFileName);

                using (var fileStream = new FileStream(physicalFilePath, FileMode.Create))
                {
                    await model.ProfilePictureFile.CopyToAsync(fileStream);
                }

                if (!string.IsNullOrEmpty(user.ProfilePicturePath) &&
                    user.ProfilePicturePath.StartsWith(ProfilePictureUrlPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string oldFileName = Path.GetFileName(user.ProfilePicturePath);
                    string oldPhysicalPath = Path.Combine(uploadsFolderPath, oldFileName);

                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }

                newProfilePicturePath = ProfilePictureUrlPrefix + uniqueFileName;
            }

            // ==========================
            // Update editable fields
            // ==========================

            user.FirstName = model.FirstName;
            user.MiddleName = model.MiddleName;
            user.LastName = model.LastName;

            user.ContactNumber = model.ContactNumber;

            user.Gender = model.Gender;
            user.Birthdate = model.Birthdate;

            user.Province = model.Province;
            user.City = model.City;
            user.Barangay = model.Barangay;
            user.StreetAddress = model.StreetAddress;

            user.Bio = model.Bio;

            user.ProfilePicturePath = newProfilePicturePath;

            // Save the updated user through ASP.NET Identity.
            var result = await _userManager.UpdateAsync(user);
            // If saving failed, display the Identity errors.
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            // Store a one-time success message.
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";// Call from View, Less Manual Code

            // Redirect back to the Edit page.
            return RedirectToAction(nameof(Edit));
        }
    }
}