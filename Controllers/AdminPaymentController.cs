using E_Commerce.DataContext;
using E_Commerce.Dtos.Payment;
using E_Commerce.Entities;
using E_Commerce.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace E_Commerce.Controllers
{
    /// <summary>
    /// Admin-only endpoints for payment observability, audit, and manual overrides.
    /// All endpoints require Admin or Owner role.
    /// </summary>
    [ApiController]
    [Route("api/admin/payments")]
    [Authorize(Roles = "Admin,Owner")]
    public class AdminPaymentController : ControllerBase
    {
        private readonly EcommerceDbContext _db;
        private readonly ILogger<AdminPaymentController> _logger;

        public AdminPaymentController(EcommerceDbContext db, ILogger<AdminPaymentController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ═══════════════════════ List Payments ═══════════════════════

        /// <summary>
        /// GET /api/admin/payments?status=Failed&userId=5&page=1&pageSize=20
        /// Returns a paginated list of all payments with optional filters.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListPayments(
            [FromQuery] string? status,
            [FromQuery] int? userId,
            [FromQuery] int? orderId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(page, 1);

            var query = _db.Payments
                .Include(p => p.Attempts)
                .AsNoTracking()
                .AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(p => p.Status == parsedStatus);
            }

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            if (orderId.HasValue)
                query = query.Where(p => p.OrderId == orderId.Value);

            var totalCount = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminPaymentListItemDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    OrderId = p.OrderId,
                    Provider = p.Provider,
                    AmountCents = p.AmountCents,
                    Currency = p.Currency,
                    Status = p.Status.ToString(),
                    ProviderOrderId = p.ProviderOrderId,
                    ProviderTransactionId = p.ProviderTransactionId,
                    ErrorCode = p.ErrorCode,
                    ErrorMessage = p.ErrorMessage,
                    AttemptCount = p.Attempts.Count,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                data = payments,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // ═══════════════════════ Payment Detail ═══════════════════════

        /// <summary>
        /// GET /api/admin/payments/{id}
        /// Returns full payment detail including attempts and audit log.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentDetail(int id)
        {
            var payment = await _db.Payments
                .Include(p => p.Attempts)
                .Include(p => p.Order)
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound(new { message = $"Payment {id} not found." });

            var auditLogs = await _db.Set<PaymentAuditLog>()
                .Where(a => a.PaymentId == id)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            var dto = new AdminPaymentDetailDto
            {
                Id = payment.Id,
                UserId = payment.UserId,
                OrderId = payment.OrderId,
                Provider = payment.Provider,
                AmountCents = payment.AmountCents,
                Currency = payment.Currency,
                Status = payment.Status.ToString(),
                ProviderOrderId = payment.ProviderOrderId,
                ProviderTransactionId = payment.ProviderTransactionId,
                IdempotencyKey = payment.IdempotencyKey,
                ErrorCode = payment.ErrorCode,
                ErrorMessage = payment.ErrorMessage,
                Metadata = payment.Metadata,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
                OrderStatus = payment.Order?.Status.ToString(),
                UserEmail = payment.User?.Email,
                Attempts = payment.Attempts
                    .OrderBy(a => a.AttemptNo)
                    .Select(a => new AdminPaymentAttemptDto
                    {
                        Id = a.Id,
                        AttemptNo = a.AttemptNo,
                        Status = a.Status.ToString(),
                        ProviderOrderId = a.ProviderOrderId,
                        FailureReason = a.FailureReason,
                        CreatedAt = a.CreatedAt
                    }).ToList(),
                AuditLog = auditLogs
                    .Select(a => new AdminPaymentAuditDto
                    {
                        Action = a.Action,
                        AdminUserId = a.AdminUserId?.ToString(),
                        Reason = a.Reason,
                        PreviousStatus = a.PreviousStatus,
                        NewStatus = a.NewStatus,
                        Timestamp = a.Timestamp
                    }).ToList()
            };

            return Ok(dto);
        }

        // ═══════════════════════ Manual Status Override ═══════════════════════

        /// <summary>
        /// POST /api/admin/payments/{id}/override-status
        /// Allows admin to manually override a payment status with audit trail.
        /// This should only be used in exceptional circumstances (e.g. manual bank confirmation).
        /// </summary>
        [HttpPost("{id}/override-status")]
        public async Task<IActionResult> OverrideStatus(int id, [FromBody] AdminManualStatusDto request)
        {
            if (string.IsNullOrWhiteSpace(request.NewStatus) || string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { message = "NewStatus and Reason are required." });

            if (!Enum.TryParse<PaymentStatus>(request.NewStatus, true, out var newStatus))
                return BadRequest(new { message = $"Invalid payment status: {request.NewStatus}" });

            var payment = await _db.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound(new { message = $"Payment {id} not found." });

            var previousStatus = payment.Status;

            // Validate state transition (admin can force, but we still log warnings)
            if (!PaymentStateMachine.CanTransition(previousStatus, newStatus))
            {
                _logger.LogWarning(
                    "Admin forcing illegal state transition for Payment {PaymentId}: {From} → {To}. Reason: {Reason}",
                    id, previousStatus, newStatus, request.Reason);
            }

            // Apply the status change
            payment.Status = newStatus;
            payment.UpdatedAt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Egypt Standard Time");

            // Sync order status with payment status
            if (payment.Order != null)
            {
                switch (newStatus)
                {
                    case PaymentStatus.Succeeded:
                        payment.Order.Status = OrderStatus.Paid;
                        break;
                    case PaymentStatus.Failed:
                        payment.Order.Status = OrderStatus.Failed;
                        // Restore inventory
                        foreach (var item in payment.Order.Items)
                        {
                            var variant = await _db.ProductVariants.FindAsync(item.ProductVariantId);
                            if (variant != null)
                                variant.Quantity += item.Quantity;
                        }
                        break;
                    case PaymentStatus.Canceled:
                        payment.Order.Status = OrderStatus.Cancelled;
                        foreach (var item in payment.Order.Items)
                        {
                            var variant = await _db.ProductVariants.FindAsync(item.ProductVariantId);
                            if (variant != null)
                                variant.Quantity += item.Quantity;
                        }
                        break;
                    case PaymentStatus.Refunded:
                        payment.Order.Status = OrderStatus.Cancelled;
                        foreach (var item in payment.Order.Items)
                        {
                            var variant = await _db.ProductVariants.FindAsync(item.ProductVariantId);
                            if (variant != null)
                                variant.Quantity += item.Quantity;
                        }
                        break;
                }
            }

            // Create audit log entry
            var adminId = GetAdminUserId();
            var auditLog = new PaymentAuditLog
            {
                PaymentId = id,
                Action = "AdminOverride",
                AdminUserId = adminId,
                Reason = request.Reason,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = newStatus.ToString(),
                Timestamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Egypt Standard Time"),
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    previousStatus = previousStatus.ToString(),
                    newStatus = newStatus.ToString(),
                    adminId,
                    forced = !PaymentStateMachine.CanTransition(previousStatus, newStatus)
                })
            };

            _db.Set<PaymentAuditLog>().Add(auditLog);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Admin {AdminId} overrode Payment {PaymentId} status: {From} → {To}. Reason: {Reason}",
                adminId, id, previousStatus, newStatus, request.Reason);

            return Ok(new
            {
                message = $"Payment status overridden: {previousStatus} → {newStatus}",
                paymentId = id,
                previousStatus = previousStatus.ToString(),
                newStatus = newStatus.ToString(),
                auditLogId = auditLog.Id
            });
        }

        // ═══════════════════════ Helper ═══════════════════════

        private int? GetAdminUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out var userId))
                return userId;
            return null;
        }
    }
}
