using PETHUB.Models;

namespace PETHUB.ViewModels
{
    /// <summary>
    /// Supplies all information required by the
    /// Admin My Posts page.
    /// </summary>
    public class AdminMyPostsViewModel
    {
        // Reuses the existing profile ViewModel so the
        // profile card/sidebar remains consistent.
        public EditProfileViewModel Profile { get; set; } = new();

        // Stores all PetFeed posts created by
        // the currently logged-in administrator.
        public List<PetFeed> Posts { get; set; } = new();
    }
}