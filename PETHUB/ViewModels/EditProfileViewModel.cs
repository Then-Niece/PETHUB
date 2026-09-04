using PETHUB.Models;
using System.ComponentModel.DataAnnotations;
using PETHUB.Validation;

namespace PETHUB.ViewModels
{
    public class EditProfileViewModel
    {
        // ==========================
        // Editable Fields
        // ==========================

        // Member's first name.
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        // Optional middle name.
        [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters.")]
        public string? MiddleName { get; set; }

        // Member's last name.
        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; }

        // Member's contact number.
        [Required(ErrorMessage = "Contact number is required.")]
        [RegularExpression(@"^[0-9]{11}$", ErrorMessage = "Contact number must be exactly 11 digits.")]
        public string ContactNumber { get; set; }

        // Member's selected gender.
        public string? Gender { get; set; }

        // Member's date of birth.
        [Required(ErrorMessage = "Birthdate is required.")]
        [DataType(DataType.Date)]
        [MinimumAge(18, ErrorMessage = "You must be at least 18 years old.")]
        public DateTime? Birthdate { get; set; }

        // Province where the member currently resides.
        [Required(ErrorMessage = "Province is required.")]
        public string Province { get; set; }

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Barangay is required.")]
        public string Barangay { get; set; }

        [StringLength(200)]
        public string? StreetAddress { get; set; }

        // Short biography displayed on the user's profile.
        [StringLength(500, ErrorMessage = "Biography cannot exceed 500 characters.")]
        public string? Bio { get; set; }

        // ==========================
        // Read-only Information
        // ==========================

        // Registered email address.
        public string Email { get; set; }

        // Date and time when the account was created.
        public DateTime CreatedAt { get; set; }

        // Current account status (Active or Inactive).
        public UserStatus Status { get; set; }

        // Date when the user accepted the Terms and Conditions.
        public DateTime? AcceptedTermsDate { get; set; }

        // Stores the relative path of the uploaded Valid ID image.
        public string? IdPhotoPath { get; set; }

        // Holds the uploaded Valid ID image during form submission.
        // This is not stored in the database and is only used for file upload.
        public IFormFile? IdPhotoFile { get; set; }

        // Stores the relative path of the user's profile picture.
        public string? ProfilePicturePath { get; set; }

        // Holds the uploaded profile picture during form submission.
        // This is not stored in the database and is only used for file upload.
        public IFormFile? ProfilePictureFile { get; set; }

        // =========================================================
        // MEMBER ONLY PROFILE STATISTICS
        // =========================================================

        // Total Marketplace Listings created by this member.
        public int MarketplaceListingsCount { get; set; }

        // Total Lost & Found reports created by this member.
        public int LostFoundReportsCount { get; set; }

        // Total pets successfully sold.
        public int PetsSoldCount { get; set; }

        // Total pets successfully adopted.
        public int PetsAdoptedCount { get; set; }

        // Total resolved Lost & Found reports.
        public int ResolvedReportsCount { get; set; }
        
        public bool RemoveProfilePicture { get; set; }

    }
}