using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class SavedItemsViewModel
    {
        public List<SavedListing> SavedListings { get; set; } = new();

        public List<SavedLostFound> SavedLostFounds { get; set; } = new();

        public List<SavedPetFeed> SavedPetFeeds { get; set; } = new();

        public int TotalSaved =>
            SavedListings.Count
            + SavedLostFounds.Count
            + SavedPetFeeds.Count;
    }
}