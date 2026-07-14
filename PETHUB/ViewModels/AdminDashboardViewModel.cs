namespace PETHUB.ViewModels
{
    public class AdminDashboardViewModel
    {
        // 1. Overview
        public int TotalMembers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalParticipants { get; set; }

        // 2. Pending Approvals
        public int PendingMarketplaceListings { get; set; }
        public int PendingLostAndFoundPosts { get; set; }

        // 3. Pending Reports
        public int PendingMarketplaceReports { get; set; }
        public int PendingLostAndFoundReports { get; set; }
    }
}
