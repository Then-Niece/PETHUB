using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class MyPostsViewModel
    {
        // Combined Marketplace and Lost & Found posts.
        public List<(Listing? Listing, LostFound? Report)> Posts { get; set; } = new(); 

        // Stores the selected type filter.
        public string? TypeFilter { get; set; }

        // Stores the selected status filter.
        public string? StatusFilter { get; set; }
    }
}
