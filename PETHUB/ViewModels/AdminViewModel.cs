using PETHUB.Models;
using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class AdminViewModel
    {
        // Purpose: Used only in UsersController Create/Edit views.
        // Captures Admin fields from the form, then maps to ApplicationUser.

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

    }


}
