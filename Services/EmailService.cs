namespace MyApp.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendAsync(string to, string subject, string body)
        {
            // TEMP email implementation (SendGrid removed, net10 compatible)
            Console.WriteLine("=== EMAIL (SIMULATED) ===");
            Console.WriteLine($"TO: {to}");
            Console.WriteLine($"SUBJECT: {subject}");
            Console.WriteLine($"BODY:\n{body}");
            Console.WriteLine("========================");

            await Task.CompletedTask;
        }
    }
}