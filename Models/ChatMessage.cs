namespace MyApp.Models
{
    public class ChatMessage
    {

        public Guid Id { get; set; }

        public Guid ChatId { get; set; }
        public Chat Chat { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";

        public DateTime Timestamp { get; set; }


    }
}
