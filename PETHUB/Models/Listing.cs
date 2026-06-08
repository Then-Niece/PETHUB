
namespace PETHUB.Models
{
    public class Listing
    {
        public int ListingId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public DateTime DatePosted { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Navigation property: one listing can have many images
        public ICollection<ListingImage> Images { get; set; }
    }

}
