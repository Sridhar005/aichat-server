namespace MyApp.Models
{
    public class SendMessageResponse
    {

        public Guid ChatId { get; set; }
        public string ChatTitle { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;

    }
}
