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
        public DateTime? Birthdate { get; set; }

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
