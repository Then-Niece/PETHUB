using PETHUB.Models;
using System.ComponentModel.DataAnnotations;
using PETHUB.Validation;

namespace PETHUB.ViewModels
{
    public class MemberViewModel
    {
        // Purpose: Used only in MembersController Create/Edit views.
        // Captures Member fields (extra ones) from the form, then maps to ApplicationUser.

        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // plain password for form
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Contact number must be exactly 11 digits.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Contact number must contain only numbers.")]
        public string ContactNumber { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;

        // Member-only fields
        [Required(ErrorMessage = "Province is required.")]
        public string Province { get; set; }

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Barangay is required.")]
        public string Barangay { get; set; }

        [StringLength(200)]
        public string? StreetAddress { get; set; }
        public string Gender { get; set; }

        [Required(ErrorMessage = "Birthdate is required.")]
        [DataType(DataType.Date)]
        [MinimumAge(18, ErrorMessage = "Member must be at least 18 years old.")]
        public DateTime? Birthdate { get; set; }

        public IFormFile? IdPhoto { get; set; } // for ID photo upload in form when admin tries to add a member
    }

}
