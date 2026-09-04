using PETHUB.Models;
using PETHUB.Validation;
using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class EditMemberViewModel
    {
        public string Id { get; set; }


        // =========================================================
        // ACCOUNT INFORMATION
        // =========================================================

        [Required(ErrorMessage = "Username is required.")]
        public string UserName { get; set; }


        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }


        // =========================================================
        // PERSONAL INFORMATION
        // =========================================================

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        public string FirstName { get; set; }


        public string? MiddleName { get; set; }


        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        public string LastName { get; set; }


        [Required(ErrorMessage = "Contact number is required.")]
        [RegularExpression(@"^[0-9]{11}$", ErrorMessage = "Contact number must be exactly 11 digits.")]
        public string ContactNumber { get; set; }


        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }


        [Required(ErrorMessage = "Birthdate is required.")]
        [DataType(DataType.Date)]
        [MinimumAge(18, ErrorMessage = "Member must be at least 18 years old.")]
        public DateTime? Birthdate { get; set; }


        // =========================================================
        // ADDRESS
        // =========================================================

        [Required(ErrorMessage = "Province is required.")]
        public string Province { get; set; }


        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }


        [Required(ErrorMessage = "Barangay is required.")]
        public string Barangay { get; set; }


        [StringLength(200)]
        public string? StreetAddress { get; set; }


        // =========================================================
        // READ-ONLY DISPLAY INFORMATION
        // =========================================================

        public UserStatus Status { get; set; }

        public string? IdPhotoPath { get; set; }
    }
}