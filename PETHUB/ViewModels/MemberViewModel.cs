using PETHUB.Models;
using System.ComponentModel.DataAnnotations;

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
        public string Address { get; set; }
        public string Gender { get; set; }

        [CustomValidation(typeof(MemberViewModel), nameof(ValidateAdultBirthdate))]
        public DateTime Birthdate { get; set; }

        // Enforced on the server for members created by an administrator too.
        public static ValidationResult? ValidateAdultBirthdate(DateTime birthdate, ValidationContext context)
        {
            var today = DateTime.Today;
            var age = today.Year - birthdate.Year;
            if (birthdate.Date > today.AddYears(-age)) age--;
            return birthdate.Date > today || age < 18
                ? new ValidationResult("Members must be at least 18 years old.")
                : ValidationResult.Success;
        }
    }

}
