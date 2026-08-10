namespace PETHUB.ViewModels
{
    // This ViewModel contains only the values needed by the Admin Dashboard.
    // Keeping dashboard data in one ViewModel prevents the Razor view from
    // directly querying the database and keeps database logic inside the controller.
    public class AdminDashboardViewModel
    {
        // ---------------------------------------------------------
        // PLATFORM STATISTICS
        // ---------------------------------------------------------

        // Total number of users assigned to the Member role.
        // This is separate from TotalAdmins because ApplicationUser represents
        // both Members and Administrators.
        public int TotalMembers { get; set; }

        // Total number of users assigned to the Admin role.
        public int TotalAdmins { get; set; }

        // Total number of Marketplace listings stored in the database.
        // This includes Pending, Approved, Rejected, Sold, and Adopted listings.
        public int TotalMarketplaceListings { get; set; }

        // Total number of Lost & Found reports stored in the database.
        // This is independent of whether a report is Pending, Approved,
        // Rejected, Active, or Resolved.
        public int TotalLostFoundReports { get; set; }

        // Total number of PetFeed posts created by administrators.
        public int TotalPetFeedPosts { get; set; }


        // ---------------------------------------------------------
        // MARKETPLACE PLATFORM SUMMARY
        // ---------------------------------------------------------

        // Number of Marketplace listings awaiting administrator approval.
        // This value will also be displayed in the Approval Queue.
        public int MarketplacePending { get; set; }

        // Number of Marketplace listings approved by administrators.
        public int MarketplaceApproved { get; set; }

        // Number of Marketplace listings rejected by administrators.
        public int MarketplaceRejected { get; set; }


        // ---------------------------------------------------------
        // LOST & FOUND PLATFORM SUMMARY
        // ---------------------------------------------------------

        // Number of Lost & Found reports awaiting administrator approval.
        // This value will also be displayed in the Approval Queue.
        public int LostFoundPending { get; set; }

        // Number of Lost & Found reports approved by administrators.
        public int LostFoundApproved { get; set; }

        // Number of Lost & Found reports rejected by administrators.
        public int LostFoundRejected { get; set; }
    }
}