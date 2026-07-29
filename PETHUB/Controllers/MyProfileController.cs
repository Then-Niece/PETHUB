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

        // Receives required services through Dependency Injection.
        // ASP.NET Core automatically provides these services at runtime.
        public MyProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
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
    }
}