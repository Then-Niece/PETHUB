using Microsoft.AspNetCore.Identity;

namespace PETHUB.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Common fields
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        // Optional middle name
        public string? MiddleName { get; set; }

        public string? ContactNumber { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;

        // Store registration date for  admins and members
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Member-only fields (nullable for Admins)
        public string? Gender { get; set; }
        public DateTime? Birthdate { get; set; }

        // New property for the date when terms were accepted.
        public DateTime? AcceptedTermsDate { get; set; }

        // New property for Member ID
        // This property is nullable because Admins may not have a Member ID.
        public string? IdPhotoPath { get; set; }

        // Stores the uploaded profile picture file path
        public string? ProfilePicturePath { get; set; }

        // Short biography displayed on the user's profile
        public string? Bio { get; set; }

        // Specific address fields
        public string? Province { get; set; }

        public string? City { get; set; }

        public string? Barangay { get; set; }

        public string? StreetAddress { get; set; }

        // Navigation property for related notifications
        public ICollection<Notification>? Notifications { get; set; }

        public string ThemePreference { get; set; } = "Light";
    }

    public enum UserStatus
    {
        Pending,
        Active,
        Inactive
    }

}
