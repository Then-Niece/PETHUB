using PETHUB.Models;
using System.ComponentModel.DataAnnotations;

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
        [Phone(ErrorMessage = "Please enter a valid contact number.")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters.")]
        public string ContactNumber { get; set; }

        // Member's selected gender.
        public string? Gender { get; set; }

        // Member's date of birth.
        public DateTime? Birthdate { get; set; }

        // Province where the member currently resides.
        [StringLength(100, ErrorMessage = "Province cannot exceed 100 characters.")]
        public string? Province { get; set; }

        // City or municipality where the member resides.
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        // Barangay of the member's current address.
        [StringLength(100, ErrorMessage = "Barangay cannot exceed 100 characters.")]
        public string? Barangay { get; set; }

        // Complete street address of the member.
        [StringLength(200, ErrorMessage = "Street address cannot exceed 200 characters.")]
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
    }
}