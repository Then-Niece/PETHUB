using PETHUB.Models;

namespace PETHUB.ViewModels
{
    // Keeps both moderation queues on one admin approval page.
    public class ApprovalDashboardViewModel
    {
        public IEnumerable<Listing> Listings { get; set; } = Enumerable.Empty<Listing>();
        public IEnumerable<LostFound> LostFounds { get; set; } = Enumerable.Empty<LostFound>();
        public string? StatusFilter { get; set; }
        public string? PetTypeFilter { get; set; }
    }
}
