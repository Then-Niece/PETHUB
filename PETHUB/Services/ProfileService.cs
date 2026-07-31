using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

namespace PETHUB.Services
{
    /// <summary>
    /// Builds reusable profile information used throughout the application.
    /// </summary>
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _context;

        public ProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EditProfileViewModel> BuildProfileViewModelAsync(ApplicationUser user)
        {
            var model = new EditProfileViewModel
            {
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

                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Status = user.Status,
                AcceptedTermsDate = user.AcceptedTermsDate,

                ProfilePicturePath = user.ProfilePicturePath,
                IdPhotoPath = user.IdPhotoPath
            };

            // ==========================================================
            // PROFILE STATISTICS
            // ==========================================================

            // Total Marketplace Listings created by this member.
            model.MarketplaceListingsCount =
                await _context.Listings.CountAsync(l =>
                    l.MemberId == user.Id);

            // Total Lost & Found reports created by this member.
            model.LostFoundReportsCount =
                await _context.LostFounds.CountAsync(r =>
                    r.UserId == user.Id);

            // Total Marketplace Listings marked as Sold.
            model.PetsSoldCount =
                await _context.Listings.CountAsync(l =>
                    l.MemberId == user.Id &&
                    l.ListStatus == ListingStatus.Sold);

            // Total Marketplace Listings marked as Adopted.
            model.PetsAdoptedCount =
                await _context.Listings.CountAsync(l =>
                    l.MemberId == user.Id &&
                    l.ListStatus == ListingStatus.Adopted);

            // Total Lost & Found reports marked as Resolved.
            model.ResolvedReportsCount =
                await _context.LostFounds.CountAsync(r =>
                    r.UserId == user.Id &&
                    r.RStatus == ReportStatus.Resolved);

            return model;
        }

    }


}