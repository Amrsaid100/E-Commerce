using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Services.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject is required.", nameof(subject));

            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Body is required.", nameof(body));

            var smtpHost = _config["Email:SmtpHost"]?.Trim();
            var smtpPortStr = _config["Email:SmtpPort"]?.Trim();
            var smtpUser = _config["Email:SmtpUser"]?.Trim();
            var smtpPass = _config["Email:SmtpPass"];
            var fromEmail = _config["Email:FromEmail"]?.Trim();

            // Validate all required configs
            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                _logger.LogError("Email service not configured: SmtpHost is missing");
                throw new InvalidOperationException("SMTP is not configured: Email:SmtpHost is missing. Please configure it in appsettings.Development.json");
            }

            if (smtpHost.Equals("smtp.example.com", StringComparison.OrdinalIgnoreCase) ||
                smtpHost.EndsWith("example.com", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Email service using placeholder SMTP host");
                throw new InvalidOperationException("SMTP is not configured (placeholder host). Update Email:SmtpHost in appsettings.Development.json with real Gmail address.");
            }

            if (!int.TryParse(smtpPortStr, out var smtpPort))
            {
                _logger.LogWarning("Invalid SMTP port, using default 587");
                smtpPort = 587;
            }

            if (string.IsNullOrWhiteSpace(smtpUser))
            {
                _logger.LogError("Email service not configured: SmtpUser is missing");
                throw new InvalidOperationException("SMTP is not configured: Email:SmtpUser is missing. Please add your Gmail address to appsettings.Development.json");
            }

            if (string.IsNullOrWhiteSpace(smtpPass))
            {
                _logger.LogError("Email service not configured: SmtpPass is missing");
                throw new InvalidOperationException("SMTP is not configured: Email:SmtpPass is missing. Please add your Gmail App Password to appsettings.Development.json");
            }

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("Email service not configured: FromEmail is missing");
                throw new InvalidOperationException("SMTP is not configured: Email:FromEmail is missing. Please configure it in appsettings.Development.json");
            }

            try
            {
                _logger.LogInformation($"Sending email to {toEmail} via SMTP host {smtpHost}");

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 10000 // 10 seconds
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "E-Commerce App"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };

                mailMessage.To.Add(toEmail.Trim());

                await client.SendMailAsync(mailMessage);
                
                _logger.LogInformation($"Email successfully sent to {toEmail}");
            }
            catch (SmtpAuthenticationException ex)
            {
                _logger.LogError($"SMTP Authentication failed: {ex.Message}. Check SmtpUser and SmtpPass (Gmail App Password)");
                throw new InvalidOperationException(
                    "Email authentication failed. Make sure you're using Gmail App Password (not regular password). " +
                    "Enable 2FA and generate App Password from myaccount.google.com/apppasswords",
                    ex
                );
            }
            catch (SmtpException ex)
            {
                _logger.LogError($"SMTP error sending email: {ex.Message}");
                throw new InvalidOperationException(
                    $"Failed to send email via SMTP. Host='{smtpHost}', Port={smtpPort}. " +
                    "Check network connectivity and SMTP configuration.",
                    ex
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error sending email: {ex.Message}");
                throw new InvalidOperationException("Failed to send email. Please check logs for details.", ex);
            }
        }
    }
}
