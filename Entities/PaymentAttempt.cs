using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public class PaymentAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PaymentId { get; set; }

        [Required]
        public int AttemptNo { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Created;

        /// <summary>
        /// Paymob order ID for this attempt
        /// </summary>
        [MaxLength(100)]
        public string? ProviderOrderId { get; set; }

        /// <summary>
        /// Payment key (token) for this attempt - short-lived, stored for reference
        /// </summary>
        [MaxLength(2000)]
        public string? ProviderPaymentKey { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(PaymentId))]
        public Payment? Payment { get; set; }
    }
}
