using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class EditProfileViewModel
    {
        // ==========================
        // Editable Fields
        // ==========================

        public string FirstName { get; set; }

        public string? MiddleName { get; set; }

        public string LastName { get; set; }

        public string ContactNumber { get; set; }

        public string? Gender { get; set; }

        public DateTime? Birthdate { get; set; }

        public string? Province { get; set; }

        public string? City { get; set; }

        public string? Barangay { get; set; }

        public string? StreetAddress { get; set; }

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
    }
}