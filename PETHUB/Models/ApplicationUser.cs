using Microsoft.AspNetCore.Identity;

namespace PETHUB.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Common fields
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ContactNumber { get; set; }
        public string Status { get; set; } = "Active";


        // Member-only fields (nullable for Admins)
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthdate { get; set; }
    }
}
