using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public enum PetFeedType
    {
        Announcement,

        [Display(Name = "Pet Tip")]
        PetTip
    }

    public class PetFeed
    {
        [Key]
        public int PetFeedId { get; set; }
        [Required]
        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime DateCreated { get; set; }

        public PetFeedType Type { get; set; }

        public ICollection<PetFeedImage>? Images { get; set; }

        public ICollection<PetFeedComment>? Comments { get; set; }

        public ICollection<SavedPetFeed>? SavedByMembers { get; set; }

        public ICollection<PetFeedPaw>? Paws { get; set; }

        // Admin who posted
        public string AdminId { get; set; }
        public ApplicationUser Admin { get; set; }

    }
}
