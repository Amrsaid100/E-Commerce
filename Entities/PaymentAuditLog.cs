using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    /// <summary>
    /// Audit log for payment state changes, especially admin manual overrides.
    /// Tracks who changed what, when, and why.
    /// </summary>
    public class PaymentAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PaymentId { get; set; }

        /// <summary>
        /// What action was performed: WebhookSuccess, WebhookFailed, AdminOverride, Refund, Timeout, Retry, etc.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = default!;

        /// <summary>
        /// User ID of admin who performed the action (null for system/webhook actions)
        /// </summary>
        public int? AdminUserId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(50)]
        public string? PreviousStatus { get; set; }

        [MaxLength(50)]
        public string? NewStatus { get; set; }

        /// <summary>
        /// Additional context (JSON) — e.g. webhook transaction ID, refund amount, etc.
        /// </summary>
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(PaymentId))]
        public Payment? Payment { get; set; }
    }
}
