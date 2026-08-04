using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

namespace PETHUB.ViewComponents
{
    public class NavigationViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        // Builds the reusable profile information.
        private readonly IProfileService _profileService;

        public NavigationViewComponent(
            UserManager<ApplicationUser> userManager,
            IProfileService profileService)
        {
            _userManager = userManager;
            _profileService = profileService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Retrieve the currently logged-in user.
            var user = await _userManager.GetUserAsync(HttpContext.User);

            // If no user is logged in, return an empty navigation model.
            if (user == null)
            {
                return View(new NavigationViewModel());
            }

            // Build the user's reusable profile information.
            var profile = await _profileService.BuildProfileViewModelAsync(user);

            // Create the navigation model.
            var model = new NavigationViewModel
            {
                FullName = $"{user.FirstName} {user.LastName}",
                ProfilePicturePath = profile.ProfilePicturePath,

                // We will improve this later.
                Role = ""
            };

            return View(model);
        }
    }
}