using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class PublicLandingPageViewModel
    {
        public List<Listing> MarketplaceListings { get; set; } = new();

        public List<LostFound> LostFoundReports { get; set; } = new();
    }
}