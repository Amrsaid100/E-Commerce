using E_Commerce.DataContext;
using E_Commerce.Entities;
using E_Commerce.Helpers;
using E_Commerce.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Services.PayMob
{
    /// <summary>
    /// Background hosted service that runs periodically to expire stale payments.
    /// 
    /// If a payment remains in CREATED or REQUIRES_ACTION for longer than the 
    /// configured timeout (default 30 minutes), it is marked as FAILED and 
    /// inventory reservations are released.
    ///
    /// This prevents inventory from being locked indefinitely when users abandon
    /// the payment iframe or when webhooks are never received.
    /// </summary>
    public class PaymentTimeoutService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentTimeoutService> _logger;

        /// <summary>
        /// How long a payment can stay in non-terminal state before being expired.
        /// </summary>
        private static readonly TimeSpan PaymentTimeout = TimeSpan.FromMinutes(30);

        /// <summary>
        /// How often to check for stale payments.
        /// </summary>
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

        public PaymentTimeoutService(
            IServiceScopeFactory scopeFactory,
            ILogger<PaymentTimeoutService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentTimeoutService started. Checking every {Interval} for payments older than {Timeout}.",
                CheckInterval, PaymentTimeout);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpireStalePaymentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PaymentTimeoutService cycle");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task ExpireStalePaymentsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EcommerceDbContext>();

            var cutoff = DateTime.UtcNow - PaymentTimeout;

            // Find payments that are still pending and older than the timeout
            var stalePayments = await db.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Items)
                .Include(p => p.Attempts)
                .Where(p =>
                    (p.Status == PaymentStatus.Created || p.Status == PaymentStatus.RequiresAction) &&
                    p.CreatedAt < cutoff)
                .ToListAsync(ct);

            if (stalePayments.Count == 0)
                return;

            _logger.LogInformation("Found {Count} stale payment(s) to expire", stalePayments.Count);

            foreach (var payment in stalePayments)
            {
                try
                {
                    // Validate state machine transition before changing status
                    if (!PaymentStateMachine.CanTransition(payment.Status, PaymentStatus.Failed))
                    {
                        _logger.LogWarning("Cannot transition Payment {PaymentId} from {Status} → Failed (state machine), skipping.",
                            payment.Id, payment.Status);
                        continue;
                    }

                    payment.Status = PaymentStatus.Failed;
                    payment.ErrorCode = "TIMEOUT";
                    payment.ErrorMessage = $"Payment expired after {PaymentTimeout.TotalMinutes} minutes without completion.";
                    payment.UpdatedAt = DateTime.UtcNow;

                    // Update the latest attempt
                    var latestAttempt = payment.Attempts
                        .OrderByDescending(a => a.AttemptNo)
                        .FirstOrDefault();
                    if (latestAttempt != null)
                    {
                        latestAttempt.Status = PaymentStatus.Failed;
                        latestAttempt.FailureReason = "Payment timeout";
                    }

                    // Release inventory if order exists
                    if (payment.Order != null &&
                        payment.Order.Status == OrderStatus.PendingPayment)
                    {
                        payment.Order.Status = OrderStatus.Failed;

                        foreach (var item in payment.Order.Items)
                        {
                            var variant = await db.ProductVariants.FindAsync(new object[] { item.ProductVariantId }, ct);
                            if (variant != null)
                            {
                                variant.Quantity += item.Quantity;
                                _logger.LogInformation(
                                    "Timeout: Restored stock for variant {VariantId}: +{Qty} (Payment {PaymentId}, Order {OrderId})",
                                    variant.Id, item.Quantity, payment.Id, payment.OrderId);
                            }
                        }
                    }

                    _logger.LogWarning(
                        "Payment {PaymentId} expired (Order {OrderId}). Created at {CreatedAt}, timeout after {Minutes} min.",
                        payment.Id, payment.OrderId, payment.CreatedAt, PaymentTimeout.TotalMinutes);

                    // Audit log for timeout
                    db.Set<PaymentAuditLog>().Add(new PaymentAuditLog
                    {
                        PaymentId = payment.Id,
                        Action = "Timeout",
                        Reason = $"Payment expired after {PaymentTimeout.TotalMinutes} minutes without webhook confirmation",
                        PreviousStatus = "RequiresAction",
                        NewStatus = PaymentStatus.Failed.ToString()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error expiring payment {PaymentId}", payment.Id);
                }
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Expired {Count} stale payment(s) and released inventory.", stalePayments.Count);
        }
    }
}
