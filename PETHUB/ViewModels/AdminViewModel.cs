using PETHUB.Models;
using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class AdminViewModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [StringLength(
            11,
            MinimumLength = 11,
            ErrorMessage = "Contact number must be exactly 11 digits.")]
        [RegularExpression(
            @"^\d{11}$",
            ErrorMessage = "Contact number must contain only numbers.")]
        public string ContactNumber { get; set; }

        public UserStatus Status { get; set; } = UserStatus.Active;
    }
}
