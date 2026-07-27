namespace PETHUB.Models
{
    public class PetFeedPaw
    {
        public int PetFeedPawId { get; set; }


        // The post that was liked
        public int PetFeedId { get; set; }
        public PetFeed PetFeed { get; set; }


        // The member who liked it
        public string MemberId { get; set; }
        public ApplicationUser Member { get; set; }


        public DateTime DatePawed { get; set; } = DateTime.Now;
    }
}
