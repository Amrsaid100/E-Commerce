using System.Net;
using System.Net.Mail;
using System.Text;
using E_Commerce.Entities;
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
                    Timeout = 30000 // 10 seconds
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
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email. StatusCode={StatusCode}", ex.StatusCode);

                // Gmail 5.7.0 / Authentication Required message
                if (ex.Message.Contains("5.7.0", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("Authentication", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Email authentication failed. Use a Gmail App Password (not your regular password). " +
                        "Enable 2FA then generate App Password from myaccount.google.com/apppasswords",
                        ex
                    );
                }

                throw new InvalidOperationException(
                    $"Failed to send email via SMTP. Host='{smtpHost}', Port={smtpPort}. Check SMTP settings/network.",
                    ex
                );
            }

            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error sending email: {ex.Message}");
                throw new InvalidOperationException("Failed to send email. Please check logs for details.", ex);
            }
        }

        // ═══════════════ Owner Order Notification ═══════════════

        public async Task SendOwnerNewOrderEmailAsync(Order order, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔔 SendOwnerNewOrderEmailAsync called for Order #{OrderId}", order.Id);
                
                var ownerEmail = _config["Email:OwnerNotificationEmail"]?.Trim();
                if (string.IsNullOrWhiteSpace(ownerEmail))
                {
                    _logger.LogWarning("⚠️ Owner notification email not configured (Email:OwnerNotificationEmail). Skipping order notification for Order #{OrderId}.", order.Id);
                    return;
                }

                _logger.LogInformation("📧 Preparing to send email to owner: {OwnerEmail} for Order #{OrderId}", ownerEmail, order.Id);
                
                var subject = $"🛒 New Order Received - #{order.Id}";
                var body = BuildOrderNotificationHtml(order);

                _logger.LogInformation("📨 Calling SendEmailAsync for Order #{OrderId}", order.Id);
                await SendEmailAsync(ownerEmail, subject, body);

                _logger.LogInformation("✅ Owner notification email sent successfully for Order #{OrderId} to {OwnerEmail}", order.Id, ownerEmail);
            }
            catch (Exception ex)
            {
                // CRITICAL: Never let email failure break the order flow.
                // The order is already persisted — just log and move on.
                _logger.LogError(ex, "❌ Failed to send owner notification email for Order #{OrderId}. Order was still created successfully.", order.Id);
            }
        }

        private string BuildOrderNotificationHtml(Order order)
        {
            var sb = new StringBuilder();

            var customerName = order.User?.Name ?? "N/A";
            var customerEmail = order.Email;
            var customerPhone = order.PhoneNumber ?? "N/A";
            var paymentMethod = order.PaymentMethod == PaymentMethod.CashOnDelivery
                ? "💵 Cash on Delivery"
                : "💳 Online Payment (Paymob)";
            var status = order.Status.ToString();

            // Build address
            var addressParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(order.Street)) addressParts.Add(order.Street);
            if (!string.IsNullOrWhiteSpace(order.Neighborhood)) addressParts.Add(order.Neighborhood);
            if (order.Governorate != null) addressParts.Add(order.Governorate.NameEn);
            var address = addressParts.Count > 0 ? string.Join(", ", addressParts) : "N/A";

            var itemsSubtotal = order.Items?.Sum(i => i.UnitePrice * i.Quantity) ?? 0;

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"></head>");
            sb.AppendLine("<body style=\"margin:0;padding:0;background-color:#f4f4f4;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\">");
            sb.AppendLine("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#f4f4f4;padding:20px 0;\">");
            sb.AppendLine("<tr><td align=\"center\">");
            sb.AppendLine("<table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 10px rgba(0,0,0,0.1);\">");

            // Header
            sb.AppendLine("<tr><td style=\"background-color:#1a1a2e;padding:30px;text-align:center;\">");
            sb.AppendLine("<h1 style=\"color:#ffffff;margin:0;font-size:24px;\">👑 FREE ONE</h1>");
            sb.AppendLine("<p style=\"color:#cccccc;margin:5px 0 0;font-size:14px;\">New Order Notification</p>");
            sb.AppendLine("</td></tr>");

            // Order Summary Banner
            sb.AppendLine("<tr><td style=\"background-color:#16213e;padding:15px 30px;\">");
            sb.AppendLine($"<table width=\"100%\"><tr>");
            sb.AppendLine($"<td style=\"color:#ffffff;font-size:18px;font-weight:bold;\">Order #{order.Id}</td>");
            sb.AppendLine($"<td style=\"color:#4ecca3;font-size:18px;font-weight:bold;text-align:right;\">{order.TotalAmount:N2} EGP</td>");
            sb.AppendLine($"</tr></table>");
            sb.AppendLine("</td></tr>");

            // Body Content
            sb.AppendLine("<tr><td style=\"padding:30px;\">");

            // Customer Info
            sb.AppendLine("<h3 style=\"color:#1a1a2e;border-bottom:2px solid #4ecca3;padding-bottom:8px;margin-top:0;\">👤 Customer Information</h3>");
            sb.AppendLine("<table width=\"100%\" style=\"margin-bottom:20px;\">");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;width:130px;\">Name:</td><td style=\"padding:5px 0;font-weight:600;\">{customerName}</td></tr>");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;\">Email:</td><td style=\"padding:5px 0;\">{customerEmail}</td></tr>");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;\">Phone:</td><td style=\"padding:5px 0;\">{customerPhone}</td></tr>");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;\">Address:</td><td style=\"padding:5px 0;\">{address}</td></tr>");
            sb.AppendLine("</table>");

            // Order Details
            sb.AppendLine("<h3 style=\"color:#1a1a2e;border-bottom:2px solid #4ecca3;padding-bottom:8px;\">📦 Order Items</h3>");
            sb.AppendLine("<table width=\"100%\" cellpadding=\"8\" cellspacing=\"0\" style=\"border-collapse:collapse;margin-bottom:20px;\">");
            sb.AppendLine("<thead><tr style=\"background-color:#f8f9fa;\">");
            sb.AppendLine("<th style=\"text-align:left;border-bottom:2px solid #dee2e6;padding:10px 8px;\">Product</th>");
            sb.AppendLine("<th style=\"text-align:center;border-bottom:2px solid #dee2e6;padding:10px 8px;\">Qty</th>");
            sb.AppendLine("<th style=\"text-align:right;border-bottom:2px solid #dee2e6;padding:10px 8px;\">Price</th>");
            sb.AppendLine("<th style=\"text-align:right;border-bottom:2px solid #dee2e6;padding:10px 8px;\">Subtotal</th>");
            sb.AppendLine("</tr></thead><tbody>");

            if (order.Items != null)
            {
                foreach (var item in order.Items)
                {
                    var lineTotal = item.UnitePrice * item.Quantity;
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td style=\"padding:10px 8px;border-bottom:1px solid #eee;\">{item.ProductName ?? "Unknown"}</td>");
                    sb.AppendLine($"<td style=\"padding:10px 8px;border-bottom:1px solid #eee;text-align:center;\">{item.Quantity}</td>");
                    sb.AppendLine($"<td style=\"padding:10px 8px;border-bottom:1px solid #eee;text-align:right;\">{item.UnitePrice:N2} EGP</td>");
                    sb.AppendLine($"<td style=\"padding:10px 8px;border-bottom:1px solid #eee;text-align:right;font-weight:600;\">{lineTotal:N2} EGP</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody></table>");

            // Totals
            sb.AppendLine("<table width=\"100%\" style=\"margin-bottom:20px;\">");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;\">Products Subtotal:</td><td style=\"padding:5px 0;text-align:right;\">{itemsSubtotal:N2} EGP</td></tr>");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;\">Shipping Cost:</td><td style=\"padding:5px 0;text-align:right;\">{(order.ShippingCost > 0 ? $"{order.ShippingCost:N2} EGP" : "FREE")}</td></tr>");
            sb.AppendLine($"<tr style=\"font-size:18px;font-weight:bold;\"><td style=\"padding:10px 0;border-top:2px solid #1a1a2e;\">Total Amount:</td><td style=\"padding:10px 0;text-align:right;border-top:2px solid #1a1a2e;color:#1a1a2e;\">{order.TotalAmount:N2} EGP</td></tr>");
            sb.AppendLine("</table>");

            // Payment & Status
            sb.AppendLine("<h3 style=\"color:#1a1a2e;border-bottom:2px solid #4ecca3;padding-bottom:8px;\">💳 Payment & Status</h3>");
            sb.AppendLine("<table width=\"100%\" style=\"margin-bottom:20px;\">");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;width:130px;\">Payment Method:</td><td style=\"padding:5px 0;font-weight:600;\">{paymentMethod}</td></tr>");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;\">Order Status:</td><td style=\"padding:5px 0;\"><span style=\"background-color:#fff3cd;color:#856404;padding:3px 10px;border-radius:12px;font-size:13px;\">{status}</span></td></tr>");
            sb.AppendLine($"<tr><td style=\"padding:5px 0;color:#666;\">Order Date:</td><td style=\"padding:5px 0;\">{order.CreatedAt:yyyy-MM-dd HH:mm:ss} (Egypt)</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("</td></tr>");

            // Footer
            sb.AppendLine("<tr><td style=\"background-color:#1a1a2e;padding:20px 30px;text-align:center;\">");
            sb.AppendLine("<p style=\"color:#888;margin:0;font-size:12px;\">This is an automated notification from Free One Store</p>");
            sb.AppendLine("<p style=\"color:#666;margin:5px 0 0;font-size:11px;\">Please do not reply to this email</p>");
            sb.AppendLine("</td></tr>");

            sb.AppendLine("</table>");
            sb.AppendLine("</td></tr></table>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }
    }
}
