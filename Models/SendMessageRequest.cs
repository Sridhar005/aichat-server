using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class SendMessageRequest
    {
        [Required]
        public Guid ChatId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
