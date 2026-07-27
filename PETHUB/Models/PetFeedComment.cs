using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public class PetFeedComment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime DatePosted { get; set; }= DateTime.MinValue;

        // Link to post
        public int PetFeedId { get; set; }
        public PetFeed PetFeed { get; set; }

        // Link to member
        public string MemberId { get; set; }
        public ApplicationUser Member { get; set; }
    }
}
