namespace MyApp.Models
{
    public class PasswordResetToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }   // ✅ FIXED
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; } = false; // ✅ SEE ERROR 3


    }
}
