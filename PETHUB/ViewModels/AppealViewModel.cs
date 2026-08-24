using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class AppealViewModel
    {
        // Identifies whether the appeal belongs to a Marketplace listing
        // or a Lost & Found post. Expected values are "listing" or "lostfound".
        public string PostType { get; set; } = string.Empty;

        // Identifies the existing post being appealed.
        // The ID is passed back to the controller when the Member submits.
        public int PostId { get; set; }

        // Displays the title of the existing removed post on the appeal form.
        // This is read-only information and is not saved as a duplicate post.
        public string PostTitle { get; set; } = string.Empty;

        // Stores the Member's explanation for why the removed post should
        // be reconsidered by an Admin.
        [Required(ErrorMessage = "Please explain why you believe this post should be restored.")]
        [StringLength(
            2000,
            MinimumLength = 10,
            ErrorMessage = "Your appeal must be between 10 and 2000 characters."
        )]
        public string AppealMessage { get; set; } = string.Empty;
    }
}