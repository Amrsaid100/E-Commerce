using E_Commerce.Entities;

namespace E_Commerce.Dtos.Payment
{
    // ═══════════════════ Admin: List Payments ═══════════════════

    public class AdminPaymentListItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public string Provider { get; set; } = default!;
        public long AmountCents { get; set; }
        public string Currency { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string? ProviderOrderId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int AttemptCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ═══════════════════ Admin: Payment Detail ═══════════════════

    public class AdminPaymentDetailDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public string Provider { get; set; } = default!;
        public long AmountCents { get; set; }
        public string Currency { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string? ProviderOrderId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string IdempotencyKey { get; set; } = default!;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? OrderStatus { get; set; }
        public string? UserEmail { get; set; }
        public List<AdminPaymentAttemptDto> Attempts { get; set; } = new();
        public List<AdminPaymentAuditDto> AuditLog { get; set; } = new();
    }

    public class AdminPaymentAttemptDto
    {
        public int Id { get; set; }
        public int AttemptNo { get; set; }
        public string Status { get; set; } = default!;
        public string? ProviderOrderId { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════ Admin: Audit Log Entry ═══════════════════

    public class AdminPaymentAuditDto
    {
        public string Action { get; set; } = default!;
        public string? AdminUserId { get; set; }
        public string? Reason { get; set; }
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // ═══════════════════ Admin: Manual Status Override ═══════════════════

    public class AdminManualStatusDto
    {
        public string NewStatus { get; set; } = default!;
        public string Reason { get; set; } = default!;
    }
}
