using E_Commerce.Dtos.Payment;
using E_Commerce.Entities;

namespace E_Commerce.Services.PayMob
{
    /// <summary>
    /// High-level payment business logic.
    /// </summary>
    public interface IPaymobPaymentService
    {
        /// <summary>
        /// Create a Paymob payment session for an order.
        /// Handles idempotency: same key returns same session if still valid.
        /// </summary>
        Task<PaymobSessionResponseDto> CreateSessionAsync(int orderId, int userId, string? idempotencyKey);

        /// <summary>
        /// Get payment status for polling from frontend.
        /// </summary>
        Task<PaymentStatusDto> GetPaymentStatusAsync(int paymentId, int userId);

        /// <summary>
        /// Process a Paymob webhook callback (HMAC-verified upstream).
        /// Idempotent: repeated calls with same transaction ID are safe.
        /// </summary>
        Task ProcessWebhookAsync(PaymobTransactionObj transaction);

        /// <summary>
        /// Admin: request a refund on a payment.
        /// </summary>
        Task<RefundResponseDto> RefundAsync(int paymentId, long? amountCents, string? reason);

        /// <summary>
        /// Retry a failed payment by creating a new attempt.
        /// </summary>
        Task<PaymobSessionResponseDto> RetryPaymentAsync(int paymentId, int userId);
    }
}
