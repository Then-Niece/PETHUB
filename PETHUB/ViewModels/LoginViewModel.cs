using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username or Email")]
        public string UserNameOrEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}
