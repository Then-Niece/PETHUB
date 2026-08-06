using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Services
{
    /// <summary>
    /// Builds reusable profile information used throughout the application.
    /// </summary>
    public class AdminProfileService : AdminIProfileService
    {
        private readonly ApplicationDbContext _context;

        public AdminProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminEditProfileViewModel> BuildAdminProfileViewModelAsync(ApplicationUser user)
        {
            var model = new AdminEditProfileViewModel
            {
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                ContactNumber = user.ContactNumber,
                Bio = user.Bio,

                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Status = user.Status,

                ProfilePicturePath = user.ProfilePicturePath,
                IdPhotoPath = user.IdPhotoPath
            };

            // ==========================================================
            // ADMIN PROFILE STATISTICS
            // ==========================================================

            // Total PetFeed posts created by this administrator.
            model.PetFeedPostsCount =
                await _context.PetFeeds.CountAsync(p =>
                    p.AdminId == user.Id);

            // Total Announcement posts created by this administrator.
            model.AnnouncementsCount =
                await _context.PetFeeds.CountAsync(p =>
                    p.AdminId == user.Id &&
                    p.Type == PetFeedType.Announcement);

            // Total Pet Tip posts created by this administrator.
            model.PetTipsCount =
                await _context.PetFeeds.CountAsync(p =>
                    p.AdminId == user.Id &&
                    p.Type == PetFeedType.PetTip);

            return model;
        }

    }
}