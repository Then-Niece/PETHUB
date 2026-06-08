namespace PETHUB.Models
{
    public class ListingImage
    {
        public int ListingImageId { get; set; }   // Primary key
        public string ImagePath { get; set; }     // File path or URL

        // Foreign key to Listing
        public int ListingId { get; set; }
        public Listing Listing { get; set; }
    }

}
