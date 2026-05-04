namespace MyApp.Models
{
    public class Chat
    {

        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public User User { get; set; } = null!;
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
