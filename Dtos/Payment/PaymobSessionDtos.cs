namespace E_Commerce.Dtos.Payment
{
    /// <summary>
    /// Request to create a Paymob payment session
    /// </summary>
    public class CreatePaymobSessionDto
    {
        /// <summary>
        /// Optional idempotency key from client. If omitted, server generates one.
        /// </summary>
        public string? IdempotencyKey { get; set; }
    }

    /// <summary>
    /// Response after creating a Paymob payment session
    /// </summary>
    public class PaymobSessionResponseDto
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string IframeUrl { get; set; } = default!;
        public string IdempotencyKey { get; set; } = default!;
    }

    /// <summary>
    /// Payment status response for polling.
    /// The frontend must ONLY use displayStatus to decide UI state.
    /// NEVER show "Order confirmed" unless displayStatus == "SUCCEEDED".
    /// </summary>
    public class PaymentStatusDto
    {
        public int PaymentId { get; set; }
        public string Status { get; set; } = default!;
        public string OrderStatus { get; set; } = default!;
        public int OrderId { get; set; }
        public long AmountCents { get; set; }
        public string Currency { get; set; } = "EGP";

        /// <summary>
        /// Frontend display status: AWAITING_PAYMENT | WAITING_FOR_CONFIRMATION | SUCCEEDED | FAILED | CANCELED | TIMEOUT | REFUNDED.
        /// This is the ONLY field the frontend should use to decide what screen to show.
        /// </summary>
        public string DisplayStatus { get; set; } = default!;

        /// <summary>
        /// Safe, user-facing error reason in Arabic. Never contains internal error details.
        /// </summary>
        public string? DisplayReasonAr { get; set; }

        /// <summary>
        /// Safe, user-facing error reason in English.
        /// </summary>
        public string? DisplayReasonEn { get; set; }

        /// <summary>
        /// Raw error message (only for non-production debugging — frontend should prefer DisplayReasonAr).
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Admin refund request
    /// </summary>
    public class RefundRequestDto
    {
        public long? AmountCents { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Refund response
    /// </summary>
    public class RefundResponseDto
    {
        public int PaymentId { get; set; }
        public string Status { get; set; } = default!;
        public string Message { get; set; } = default!;
    }
}
