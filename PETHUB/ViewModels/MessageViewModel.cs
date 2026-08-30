namespace PETHUB.ViewModels
{
    public class MessageViewModel
    {
        public int MessageId { get; set; }

        public string SenderId { get; set; } = null!;

        public string? Content { get; set; }

        public List<string> ImagePaths { get; set; } = new();

        public DateTime CreatedAt { get; set; }

        public bool IsMine { get; set; }
    }
}