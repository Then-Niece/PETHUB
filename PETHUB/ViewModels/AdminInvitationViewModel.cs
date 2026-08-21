using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class AdminInvitationViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }
    }
}
