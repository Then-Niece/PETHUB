using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public class MessageImage
    {
        [Key]
        public int MessageImageId { get; set; }

        [Required]
        public string ImagePath { get; set; } = null!;

        public int MessageId { get; set; }

        public Message Message { get; set; } = null!;
    }
}