using PETHUB.Models;
using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class EditProfileViewModel
    {
        // ==========================
        // Editable Fields
        // ==========================

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters.")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [Phone(ErrorMessage = "Please enter a valid contact number.")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters.")]
        public string ContactNumber { get; set; }

        public string? Gender { get; set; }

        public DateTime? Birthdate { get; set; }

        [StringLength(100, ErrorMessage = "Province cannot exceed 100 characters.")]
        public string? Province { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "Barangay cannot exceed 100 characters.")]
        public string? Barangay { get; set; }

        [StringLength(200, ErrorMessage = "Street address cannot exceed 200 characters.")]
        public string? StreetAddress { get; set; }

        [StringLength(500, ErrorMessage = "Biography cannot exceed 500 characters.")]
        public string? Bio { get; set; }

        // ==========================
        // Read-only Information
        // ==========================

        public string Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public UserStatus Status { get; set; }

        public DateTime? AcceptedTermsDate { get; set; }

        public string? IdPhotoPath { get; set; }

        public string? ProfilePicturePath { get; set; }

        public IFormFile? ProfilePictureFile { get; set; }
    }
}