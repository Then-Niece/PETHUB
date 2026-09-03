using System.ComponentModel.DataAnnotations;
using PETHUB.Models;
using PETHUB.Validation;

namespace PETHUB.ViewModels
{
    public class EditAdminViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required.")]
        [StringLength(
            11,
            MinimumLength = 11,
            ErrorMessage = "Contact number must be exactly 11 digits."
        )]
        [RegularExpression(
            @"^\d{11}$",
            ErrorMessage = "Contact number must contain only numbers."
        )]
        public string ContactNumber { get; set; } = string.Empty;

        public UserStatus Status { get; set; }
    }
}