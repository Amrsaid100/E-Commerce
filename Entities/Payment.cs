using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public enum PaymentStatus
    {
        Created,
        RequiresAction,
        Processing,
        Succeeded,
        Failed,
        Canceled,
        Refunded,
        RefundRequested
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "paymob";

        [Required]
        public long AmountCents { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "EGP";

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Created;

        /// <summary>
        /// Paymob order ID returned from ecommerce/orders
        /// </summary>
        [MaxLength(100)]
        public string? ProviderOrderId { get; set; }

        /// <summary>
        /// Transaction ID from Paymob webhook callback
        /// </summary>
        [MaxLength(100)]
        public string? ProviderTransactionId { get; set; }

        /// <summary>
        /// Idempotency key to prevent duplicate payment sessions
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string IdempotencyKey { get; set; } = default!;

        [MaxLength(100)]
        public string? ErrorCode { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Arbitrary JSON metadata
        /// </summary>
        public string? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        public List<PaymentAttempt> Attempts { get; set; } = new();
    }
}
