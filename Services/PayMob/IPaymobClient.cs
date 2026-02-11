using E_Commerce.Entities;

namespace E_Commerce.Services.PayMob
{
    /// <summary>
    /// Low-level client for Paymob Accept API.
    /// Handles: auth token, order registration, payment key generation, HMAC verification, refund.
    /// </summary>
    public interface IPaymobClient
    {
        /// <summary>
        /// Step 1: Authenticate and get auth token from Paymob.
        /// </summary>
        Task<string> GetAuthTokenAsync();

        /// <summary>
        /// Step 2: Register an order with Paymob.
        /// Returns the Paymob order ID.
        /// </summary>
        Task<long> RegisterOrderAsync(string authToken, long amountCents, string merchantOrderId, Order order);

        /// <summary>
        /// Step 3: Request a payment key for the registered order.
        /// Returns the payment token string.
        /// </summary>
        Task<string> RequestPaymentKeyAsync(string authToken, long amountCents, long paymobOrderId, Order order);

        /// <summary>
        /// Build the iframe URL from a payment key.
        /// </summary>
        string BuildIframeUrl(string paymentKey);

        /// <summary>
        /// Verify HMAC signature on a webhook callback.
        /// Uses Paymob's specific field concatenation order.
        /// </summary>
        bool VerifyHmac(string hmacHeader, string rawBody);

        /// <summary>
        /// Attempt a refund via Paymob API.
        /// Returns (success, message).
        /// </summary>
        Task<(bool Success, string Message)> RefundTransactionAsync(string authToken, long transactionId, long amountCents);
    }
}
