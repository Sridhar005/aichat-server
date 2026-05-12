using MyApp.Services;
using System.Net;
using System.Net.Mail;

namespace AIChatApp.Services;

public class EmailService : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        // ✅ TEMP: log instead of sending
        Console.WriteLine($"EMAIL TO: {to}");
        Console.WriteLine(subject);
        Console.WriteLine(body);

        await Task.CompletedTask;
    }
}