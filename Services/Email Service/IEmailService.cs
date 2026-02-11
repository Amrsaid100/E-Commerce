using E_Commerce.Entities;

namespace E_Commerce.Services.EmailService
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);

        /// <summary>
        /// Sends an HTML email notification to the store owner when a new order is placed.
        /// This method is fire-and-forget safe — it catches all exceptions internally and logs them.
        /// </summary>
        Task SendOwnerNewOrderEmailAsync(Order order, CancellationToken cancellationToken = default);
    }
}
