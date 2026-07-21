namespace PETHUB.Models
{
    public class PetFeedImage
    {
        public int PetFeedImageId { get; set; }   // Primary key
        public string ImagePath { get; set; }     // File path or URL

        // Foreign key to PetFeed
        public int PetFeedId { get; set; }
        public PetFeed PetFeed { get; set; }
    }
}
