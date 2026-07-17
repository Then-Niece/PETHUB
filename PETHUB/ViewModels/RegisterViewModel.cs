using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Contact number must be exactly 11 digits.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Contact number must contain only numbers.")]
        public string ContactNumber { get; set; }

        public string Address { get; set; }
        public string Gender { get; set; }

        [DataType(DataType.Date)]
        [CustomValidation(typeof(RegisterViewModel), nameof(ValidateAdultBirthdate))]
        public DateTime? Birthdate { get; set; }

        // Server-side validation prevents bypassing the date picker.
        public static ValidationResult? ValidateAdultBirthdate(DateTime? birthdate, ValidationContext context)
        {
            if (!birthdate.HasValue) return new ValidationResult("Birthdate is required.");
            var today = DateTime.Today;
            var age = today.Year - birthdate.Value.Year;
            if (birthdate.Value.Date > today.AddYears(-age)) age--;
            return birthdate.Value.Date > today || age < 18
                ? new ValidationResult("You must be at least 18 years old to register.")
                : ValidationResult.Success;
        }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "You must accept the Terms and Conditions.")]
        [Display(Name = "I agree to the Terms and Conditions")]
        public bool AcceptTerms { get; set; }



    }
}
