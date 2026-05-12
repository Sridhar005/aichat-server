namespace MyApp.Models
{
    public class AuthRequests
    {
        public record ForgotPasswordRequest(string Email);
        public record ResetPasswordRequest(string Token, string NewPassword);

    }
}
