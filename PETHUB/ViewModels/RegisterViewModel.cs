using System.ComponentModel.DataAnnotations;
using PETHUB.Validation;

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
        [MinimumAge(18, ErrorMessage = "You must be at least 18 years old to register.")]
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

        //make the member ID photo required for registration
        [Required(ErrorMessage = "ID photo is required.")]
        [Display(Name = "Add a Valid ID Photo")]
        public IFormFile? IdPhoto { get; set; }



    }
}
