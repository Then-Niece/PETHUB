using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class ChangeUsernameViewModel
    {
        [Required(ErrorMessage = "New username is required.")]
        public string NewUsername { get; set; } = string.Empty;
    }
}
