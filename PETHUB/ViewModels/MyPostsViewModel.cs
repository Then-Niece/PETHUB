using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class MyPostsViewModel : PaginationViewModel
    {
        // Logged-in user's profile information displayed at the top of the page.
        public EditProfileViewModel Profile { get; set; } = new();

        // Combined Marketplace and Lost & Found posts.
        public List<(Listing? Listing, LostFound? Report)> Posts { get; set; } = new();

        // Stores the selected type filter.
        public string? TypeFilter { get; set; }

        // Stores the selected status filter.
        public string? StatusFilter { get; set; }
    }
}