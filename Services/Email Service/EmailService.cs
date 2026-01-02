using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace E_Commerce.Services.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            var smtpHost = _config["Email:SmtpHost"]?.Trim();
            var smtpPortStr = _config["Email:SmtpPort"]?.Trim();
            var smtpUser = _config["Email:SmtpUser"]?.Trim();
            var smtpPass = _config["Email:SmtpPass"]; 
            var fromEmail = _config["Email:FromEmail"]?.Trim();

            // Validate config
            if (string.IsNullOrWhiteSpace(smtpHost))
                throw new InvalidOperationException("SMTP is not configured: Email:SmtpHost is missing.");

            //  placeholder smtp.example.com
            if (smtpHost.Equals("smtp.example.com", StringComparison.OrdinalIgnoreCase) ||
                smtpHost.EndsWith("example.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SMTP is not configured (placeholder host). Update Email:SmtpHost in appsettings.json.");

            if (!int.TryParse(smtpPortStr, out var smtpPort))
                smtpPort = 587;

            if (string.IsNullOrWhiteSpace(smtpUser))
                throw new InvalidOperationException("SMTP is not configured: Email:SmtpUser is missing.");

            if (string.IsNullOrWhiteSpace(smtpPass))
                throw new InvalidOperationException("SMTP is not configured: Email:SmtpPass is missing.");

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("SMTP is not configured: Email:FromEmail is missing.");

            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = subject ?? "",
                    Body = body ?? "",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail.Trim());

                await client.SendMailAsync(mailMessage);
            }
            catch (SmtpException ex)
            {
             
                throw new InvalidOperationException(
                    $"Failed to send email via SMTP. Check Email:SmtpHost/Port and network/DNS. Host='{smtpHost}', Port={smtpPort}.",
                    ex
                );
            }
        }
    }
}
