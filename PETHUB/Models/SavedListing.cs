namespace PETHUB.Models
{
    public class SavedListing
    {
        public int SavedListingId { get; set; }


        // =========================================================
        // MEMBER
        // =========================================================

        public string MemberId { get; set; } = string.Empty;

        public ApplicationUser? Member { get; set; }


        // =========================================================
        // MARKETPLACE LISTING
        // =========================================================

        public int ListingId { get; set; }

        public Listing? Listing { get; set; }


        // =========================================================
        // DATE SAVED
        // =========================================================

        public DateTime DateSaved { get; set; } = DateTime.Now;
    }
}