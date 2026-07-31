using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Services
{
    /// <summary>
    /// Provides profile information and statistics for a user.
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Builds a complete profile view model including profile statistics.
        /// </summary>
        Task<EditProfileViewModel> BuildProfileViewModelAsync(ApplicationUser user);
    }
}