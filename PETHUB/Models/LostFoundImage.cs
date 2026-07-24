namespace PETHUB.Models
{
    public class LostFoundImage
    {
        public int LostFoundImageId { get; set; }   // Primary key
        public string ImagePath { get; set; }     // File path or URL

        // Foreign key to Listing
        public int LostFoundId { get; set; }
        public LostFound LostFound { get; set; }
    }
}
