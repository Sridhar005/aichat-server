using SendGrid;
using SendGrid.Helpers.Mail;

namespace MyApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("SendGrid API Key is not configured");

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(fromEmail, fromName);
            var toEmail = new EmailAddress(to);

            var message = MailHelper.CreateSingleEmail(
                from,
                toEmail,
                subject,
                body,  
                body   
            );

            var response = await client.SendEmailAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                throw new Exception($"SendGrid failed: {response.StatusCode} - {responseBody}");
            }
        }
    }
}