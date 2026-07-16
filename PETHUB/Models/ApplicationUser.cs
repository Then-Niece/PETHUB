using Microsoft.AspNetCore.Identity;

namespace PETHUB.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Common fields
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ContactNumber { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;


        // Member-only fields (nullable for Admins)
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthdate { get; set; }

        // New property for the date when terms were accepted.
        public DateTime? AcceptedTermsDate { get; set; }
        
    }

    public enum UserStatus
    {
        Active,
        Inactive
    }

}
