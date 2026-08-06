using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Services
{
    public interface AdminIProfileService
    {
        Task<AdminEditProfileViewModel> BuildAdminProfileViewModelAsync(ApplicationUser user);

    }
}
