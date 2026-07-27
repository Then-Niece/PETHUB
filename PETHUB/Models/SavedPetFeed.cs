namespace PETHUB.Models
{
    public class SavedPetFeed
    {
        public int SavedPetFeedId { get; set; }

        public int PetFeedId { get; set; }

        public PetFeed PetFeed { get; set; }

        public string MemberId { get; set; }

        public ApplicationUser Member { get; set; }
    }
}
