using E_Commerce.DataContext;
using E_Commerce.Dtos.Payment;
using E_Commerce.Entities;
using E_Commerce.Helpers;
using E_Commerce.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Services.PayMob
{
    /// <summary>
    /// High-level Paymob payment business logic:
    /// Session creation with idempotency, webhook processing, status polling, refund.
    /// 
    /// ═══════════════════ SETTLEMENT NOTE ═══════════════════
    /// Paymob (Accept) handles fund settlement automatically.
    /// When a payment SUCCEEDS, funds are held by Paymob and settled
    /// to your bank account according to your Paymob dashboard settlement
    /// schedule (typically T+1 to T+3 business days).
    /// 
    /// There is NO transferToOwner or manual payout API call needed.
    /// webhook SUCCEEDED ≠ bank settlement — it means the customer's
    /// card was charged and Paymob is holding the funds for settlement.
    /// ═══════════════════════════════════════════════════════
    /// </summary>
    public class PaymobPaymentService : IPaymobPaymentService
    {
        private readonly IPaymobClient _client;
        private readonly EcommerceDbContext _db;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PaymobPaymentService> _logger;

        public PaymobPaymentService(
            IPaymobClient client,
            EcommerceDbContext db,
            IUnitOfWork uow,
            ILogger<PaymobPaymentService> logger)
        {
            _client = client;
            _db = db;
            _uow = uow;
            _logger = logger;
        }

        // ═══════════════════════ Create Session ═══════════════════════

        public async Task<PaymobSessionResponseDto> CreateSessionAsync(int orderId, int userId, string? idempotencyKey)
        {
            // 1) Validate order exists and belongs to user
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Order {orderId} not found.");

            if (order.UserId != userId)
                throw new UnauthorizedAccessException("Order does not belong to current user.");

            if (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.Failed)
                throw new InvalidOperationException($"Order {orderId} is not in a payable state (current: {order.Status}).");

            // 2) Idempotency: check if a payment with same key already exists and is still usable
            var idemKey = idempotencyKey ?? $"order-{orderId}-{Guid.NewGuid():N}";

            var existingPayment = await _db.Payments
                .Include(p => p.Attempts)
                .FirstOrDefaultAsync(p => p.IdempotencyKey == idemKey);

            if (existingPayment != null)
            {
                // If existing payment is still in a state where the iframe can be used, return it
                if (existingPayment.Status == PaymentStatus.Created ||
                    existingPayment.Status == PaymentStatus.RequiresAction)
                {
                    var lastAttempt = existingPayment.Attempts
                        .OrderByDescending(a => a.AttemptNo)
                        .FirstOrDefault();

                    if (lastAttempt?.ProviderPaymentKey != null)
                    {
                        _logger.LogInformation("Returning existing payment session {PaymentId} for idempotency key {Key}",
                            existingPayment.Id, idemKey);

                        return new PaymobSessionResponseDto
                        {
                            PaymentId = existingPayment.Id,
                            OrderId = existingPayment.OrderId,
                            IframeUrl = _client.BuildIframeUrl(lastAttempt.ProviderPaymentKey),
                            IdempotencyKey = idemKey
                        };
                    }
                }
            }

            // 3) Create new Payment record
            var amountCents = (long)(order.TotalAmount * 100);

            var payment = new Payment
            {
                UserId = userId,
                OrderId = orderId,
                Provider = "paymob",
                AmountCents = amountCents,
                Currency = "EGP",
                Status = PaymentStatus.Created,
                IdempotencyKey = idemKey,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            // 4) Call Paymob API
            var (paymobOrderId, paymentKey) = await CallPaymobFlowAsync(payment, order, amountCents);

            // 5) Create attempt record
            var attempt = new PaymentAttempt
            {
                PaymentId = payment.Id,
                AttemptNo = 1,
                Status = PaymentStatus.RequiresAction,
                ProviderOrderId = paymobOrderId.ToString(),
                ProviderPaymentKey = paymentKey,
                CreatedAt = DateTime.UtcNow
            };

            _db.PaymentAttempts.Add(attempt);

            // 6) Update payment state (with state machine enforcement)
            PaymentStateMachine.EnsureTransition(payment.Status, PaymentStatus.RequiresAction, payment.Id);
            payment.Status = PaymentStatus.RequiresAction;
            payment.ProviderOrderId = paymobOrderId.ToString();
            payment.UpdatedAt = DateTime.UtcNow;

            // Also update order status
            order.Status = OrderStatus.PendingPayment;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Payment session created: PaymentId={PaymentId}, OrderId={OrderId}, PaymobOrder={PaymobOrderId}",
                payment.Id, orderId, paymobOrderId);

            return new PaymobSessionResponseDto
            {
                PaymentId = payment.Id,
                OrderId = orderId,
                IframeUrl = _client.BuildIframeUrl(paymentKey),
                IdempotencyKey = idemKey
            };
        }

        // ═══════════════════════ Get Status ═══════════════════════

        public async Task<PaymentStatusDto> GetPaymentStatusAsync(int paymentId, int userId)
        {
            var payment = await _db.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                throw new KeyNotFoundException($"Payment {paymentId} not found.");

            if (payment.UserId != userId)
                throw new UnauthorizedAccessException("Payment does not belong to current user.");

            var statusStr = payment.Status.ToString();
            var displayStatus = PaymobErrorCodeMapper.ToDisplayStatus(statusStr);

            // Build safe user-facing reasons
            string? reasonAr = null;
            string? reasonEn = null;
            if (payment.Status == PaymentStatus.Failed ||
                payment.Status == PaymentStatus.Canceled ||
                !string.IsNullOrWhiteSpace(payment.ErrorCode))
            {
                var (arabic, english) = PaymobErrorCodeMapper.GetUserFriendlyMessage(payment.ErrorCode, payment.ErrorMessage);
                reasonAr = arabic;
                reasonEn = english;
            }

            return new PaymentStatusDto
            {
                PaymentId = payment.Id,
                Status = statusStr,
                OrderStatus = payment.Order?.Status.ToString() ?? "Unknown",
                OrderId = payment.OrderId,
                AmountCents = payment.AmountCents,
                Currency = payment.Currency,
                DisplayStatus = displayStatus.ToString(),
                DisplayReasonAr = reasonAr,
                DisplayReasonEn = reasonEn,
                ErrorMessage = payment.ErrorMessage
            };
        }

        // ═══════════════════════ Process Webhook ═══════════════════════

        public async Task ProcessWebhookAsync(PaymobTransactionObj transaction)
        {
            var paymobOrderId = transaction.Order?.Id.ToString();
            if (string.IsNullOrWhiteSpace(paymobOrderId))
            {
                _logger.LogWarning("Webhook received with no order ID, skipping.");
                return;
            }

            // Correlation ID scope — all logs within this block include these properties
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["PaymobOrderId"] = paymobOrderId,
                ["PaymobTransactionId"] = transaction.Id,
                ["WebhookCorrelationId"] = Guid.NewGuid().ToString("N")
            }))
            {
                _logger.LogInformation("Processing webhook for Paymob order {PaymobOrderId}, transaction {TransactionId}",
                    paymobOrderId, transaction.Id);

                // Wrap in a DB transaction for atomicity
                var strategy = _db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var dbTransaction = await _db.Database.BeginTransactionAsync();
                    try
                    {
                        await ProcessWebhookCoreAsync(transaction, paymobOrderId);
                        await dbTransaction.CommitAsync();
                        _logger.LogInformation("Webhook transaction committed successfully.");
                    }
                    catch (Exception ex)
                    {
                        await dbTransaction.RollbackAsync();
                        _logger.LogError(ex, "Webhook transaction rolled back.");
                        throw;
                    }
                });
            }
        }

        private async Task ProcessWebhookCoreAsync(PaymobTransactionObj transaction, string paymobOrderId)
        {
            // Find payment by provider order ID
            var payment = await _db.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Items)
                .Include(p => p.Attempts)
                .FirstOrDefaultAsync(p => p.ProviderOrderId == paymobOrderId);

            if (payment == null)
            {
                _logger.LogWarning("No payment found for Paymob order {PaymobOrderId}", paymobOrderId);
                return;
            }

            // Idempotency: if already in terminal state with same transaction, skip
            if (payment.ProviderTransactionId == transaction.Id.ToString() &&
                PaymentStateMachine.IsFinalized(payment.Status))
            {
                _logger.LogInformation("Webhook already processed for transaction {TransactionId} (status: {Status}), skipping.",
                    transaction.Id, payment.Status);
                return;
            }

            var order = payment.Order;
            if (order == null)
            {
                _logger.LogError("Payment {PaymentId} has no associated order", payment.Id);
                return;
            }

            // ═══════════════ AMOUNT + CURRENCY VALIDATION ═══════════════
            // CRITICAL: Verify the amount Paymob charged matches our expected amount.
            // Without this, an attacker could pay 1 EGP for a 1000 EGP order.
            if (transaction.Success && !transaction.IsVoided && !transaction.IsRefunded)
            {
                if (transaction.AmountCents != payment.AmountCents)
                {
                    _logger.LogCritical(
                        "🚨 AMOUNT MISMATCH for Payment {PaymentId}! Expected {Expected} cents, got {Actual} cents. " +
                        "Paymob transaction {TransactionId}. Marking as FAILED.",
                        payment.Id, payment.AmountCents, transaction.AmountCents, transaction.Id);

                    payment.Status = PaymentStatus.Failed;
                    payment.ProviderTransactionId = transaction.Id.ToString();
                    payment.ErrorCode = "AMOUNT_MISMATCH";
                    payment.ErrorMessage = $"Amount mismatch: expected {payment.AmountCents}, got {transaction.AmountCents}";
                    payment.UpdatedAt = DateTime.UtcNow;
                    order.Status = OrderStatus.Failed;

                    foreach (var item in order.Items)
                    {
                        var variant = await _uow.ProductVariants.GetByIdAsync(item.ProductVariantId);
                        if (variant != null) variant.Quantity += item.Quantity;
                    }

                    _db.Set<PaymentAuditLog>().Add(new PaymentAuditLog
                    {
                        PaymentId = payment.Id,
                        Action = "AmountMismatch",
                        Reason = $"Expected {payment.AmountCents} {payment.Currency}, got {transaction.AmountCents} {transaction.Currency}",
                        PreviousStatus = payment.Status.ToString(),
                        NewStatus = PaymentStatus.Failed.ToString()
                    });

                    await _db.SaveChangesAsync();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(transaction.Currency) &&
                    !string.Equals(transaction.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogCritical(
                        "🚨 CURRENCY MISMATCH for Payment {PaymentId}! Expected {Expected}, got {Actual}. Marking as FAILED.",
                        payment.Id, payment.Currency, transaction.Currency);

                    payment.Status = PaymentStatus.Failed;
                    payment.ProviderTransactionId = transaction.Id.ToString();
                    payment.ErrorCode = "CURRENCY_MISMATCH";
                    payment.ErrorMessage = $"Currency mismatch: expected {payment.Currency}, got {transaction.Currency}";
                    payment.UpdatedAt = DateTime.UtcNow;
                    order.Status = OrderStatus.Failed;

                    foreach (var item in order.Items)
                    {
                        var variant = await _uow.ProductVariants.GetByIdAsync(item.ProductVariantId);
                        if (variant != null) variant.Quantity += item.Quantity;
                    }

                    _db.Set<PaymentAuditLog>().Add(new PaymentAuditLog
                    {
                        PaymentId = payment.Id,
                        Action = "CurrencyMismatch",
                        Reason = $"Expected {payment.Currency}, got {transaction.Currency}",
                        PreviousStatus = payment.Status.ToString(),
                        NewStatus = PaymentStatus.Failed.ToString()
                    });

                    await _db.SaveChangesAsync();
                    return;
                }
            }

            var previousStatus = payment.Status;

            // Update attempt
            var attempt = payment.Attempts
                .OrderByDescending(a => a.AttemptNo)
                .FirstOrDefault();

            if (transaction.Success && !transaction.IsVoided && !transaction.IsRefunded)
            {
                // ──── SUCCESS ────
                var newStatus = PaymentStatus.Succeeded;
                PaymentStateMachine.EnsureTransition(payment.Status, newStatus, payment.Id);

                payment.Status = newStatus;
                payment.ProviderTransactionId = transaction.Id.ToString();
                payment.UpdatedAt = DateTime.UtcNow;

                order.Status = OrderStatus.Paid;
                order.PaymentReference = transaction.Id.ToString();

                // Stock was already deducted during checkout, so we don't deduct again.
                // Clear cart for good measure
                var cart = await _uow.Carts.GetByUserIdAsync(order.UserId);
                if (cart != null)
                    cart.Items.Clear();

                if (attempt != null)
                    attempt.Status = PaymentStatus.Succeeded;

                _logger.LogInformation("Payment {PaymentId} SUCCEEDED. Order {OrderId} marked as Paid.",
                    payment.Id, order.Id);
            }
            else
            {
                // ──── FAILURE / VOIDED ────
                var failureReason = transaction.Data?.Message ?? "Payment failed";
                var newStatus = transaction.IsVoided ? PaymentStatus.Canceled : PaymentStatus.Failed;

                // State machine: allow transition (webhook can mark RequiresAction→Failed/Canceled)
                if (PaymentStateMachine.CanTransition(payment.Status, newStatus))
                {
                    payment.Status = newStatus;
                }
                else
                {
                    _logger.LogWarning(
                        "Webhook tried illegal transition {From} → {To} for Payment {PaymentId}, keeping current status.",
                        payment.Status, newStatus, payment.Id);
                    // Still log the attempt data but don't change status
                }

                payment.ProviderTransactionId = transaction.Id.ToString();
                payment.ErrorCode = transaction.Data?.TxnResponseCode;
                payment.ErrorMessage = failureReason;
                payment.UpdatedAt = DateTime.UtcNow;

                order.Status = transaction.IsVoided ? OrderStatus.Cancelled : OrderStatus.Failed;

                // Release inventory: restore stock
                foreach (var item in order.Items)
                {
                    var variant = await _uow.ProductVariants.GetByIdAsync(item.ProductVariantId);
                    if (variant != null)
                    {
                        variant.Quantity += item.Quantity;
                        _logger.LogInformation("Restored stock for variant {VariantId}: +{Qty}",
                            variant.Id, item.Quantity);
                    }
                }

                if (attempt != null)
                {
                    attempt.Status = payment.Status;
                    attempt.FailureReason = failureReason;
                }

                _logger.LogWarning("Payment {PaymentId} {Status}. Order {OrderId} updated. Reason: {Reason}",
                    payment.Id, payment.Status, order.Id, failureReason);
            }

            // Create audit log for webhook event
            _db.Set<PaymentAuditLog>().Add(new PaymentAuditLog
            {
                PaymentId = payment.Id,
                Action = transaction.Success ? "WebhookSuccess" : "WebhookFailed",
                Reason = transaction.Success ? "Payment completed via Paymob" : (transaction.Data?.Message ?? "Payment failed"),
                PreviousStatus = previousStatus.ToString(),
                NewStatus = payment.Status.ToString(),
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    transactionId = transaction.Id,
                    success = transaction.Success,
                    isVoided = transaction.IsVoided,
                    isRefunded = transaction.IsRefunded,
                    txnResponseCode = transaction.Data?.TxnResponseCode,
                    amountCents = transaction.AmountCents,
                    currency = transaction.Currency,
                    amountVerified = transaction.AmountCents == payment.AmountCents
                })
            });

            await _db.SaveChangesAsync();
        }

        // ═══════════════════════ Refund ═══════════════════════

        public async Task<RefundResponseDto> RefundAsync(int paymentId, long? amountCents, string? reason)
        {
            var payment = await _db.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                throw new KeyNotFoundException($"Payment {paymentId} not found.");

            if (payment.Status != PaymentStatus.Succeeded)
                throw new InvalidOperationException($"Cannot refund payment in status {payment.Status}. Only succeeded payments can be refunded.");

            if (string.IsNullOrWhiteSpace(payment.ProviderTransactionId))
            {
                // No transaction ID = mark as refund requested for manual processing
                PaymentStateMachine.EnsureTransition(payment.Status, PaymentStatus.RefundRequested, payment.Id);
                payment.Status = PaymentStatus.RefundRequested;
                payment.ErrorMessage = reason ?? "Refund requested (no transaction ID)";
                payment.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return new RefundResponseDto
                {
                    PaymentId = paymentId,
                    Status = "RefundRequested",
                    Message = "No provider transaction ID. Refund request recorded for manual processing."
                };
            }

            // Attempt actual Paymob refund
            var refundAmount = amountCents ?? payment.AmountCents;
            var authToken = await _client.GetAuthTokenAsync();
            var (success, message) = await _client.RefundTransactionAsync(
                authToken,
                long.Parse(payment.ProviderTransactionId),
                refundAmount);

            if (success)
            {
                PaymentStateMachine.EnsureTransition(payment.Status, PaymentStatus.Refunded, payment.Id);
                payment.Status = PaymentStatus.Refunded;
                payment.UpdatedAt = DateTime.UtcNow;

                if (payment.Order != null)
                    payment.Order.Status = OrderStatus.Cancelled;

                // Restore inventory
                var order = await _uow.Orders.GetByIdWithItemsAsync(payment.OrderId);
                if (order != null)
                {
                    foreach (var item in order.Items)
                    {
                        var variant = await _uow.ProductVariants.GetByIdAsync(item.ProductVariantId);
                        if (variant != null)
                            variant.Quantity += item.Quantity;
                    }
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Refund successful for payment {PaymentId}", paymentId);
                return new RefundResponseDto
                {
                    PaymentId = paymentId,
                    Status = "Refunded",
                    Message = message
                };
            }
            else
            {
                // Mark as refund requested since API refund failed
                PaymentStateMachine.EnsureTransition(payment.Status, PaymentStatus.RefundRequested, payment.Id);
                payment.Status = PaymentStatus.RefundRequested;
                payment.ErrorMessage = $"Refund attempt failed: {message}. Reason: {reason}";
                payment.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return new RefundResponseDto
                {
                    PaymentId = paymentId,
                    Status = "RefundRequested",
                    Message = $"Automatic refund failed: {message}. Recorded for manual action."
                };
            }
        }

        // ═══════════════════════ Retry ═══════════════════════

        public async Task<PaymobSessionResponseDto> RetryPaymentAsync(int paymentId, int userId)
        {
            var payment = await _db.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Items)
                .Include(p => p.Attempts)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                throw new KeyNotFoundException($"Payment {paymentId} not found.");

            if (payment.UserId != userId)
                throw new UnauthorizedAccessException("Payment does not belong to current user.");

            if (payment.Status != PaymentStatus.Failed && payment.Status != PaymentStatus.Canceled)
                throw new InvalidOperationException($"Cannot retry payment in status {payment.Status}.");

            // Validate state machine: Failed/Canceled → RequiresAction
            PaymentStateMachine.EnsureTransition(payment.Status, PaymentStatus.RequiresAction, payment.Id);

            var order = payment.Order;
            if (order == null)
                throw new InvalidOperationException("Payment has no associated order.");

            // ═══════════════ RE-DEDUCT STOCK ON RETRY ═══════════════
            // When payment failed, webhook restored stock. On retry we must re-reserve it.
            // If insufficient stock now, the retry must fail gracefully.
            foreach (var item in order.Items)
            {
                var variant = await _uow.ProductVariants.GetByIdAsync(item.ProductVariantId);
                if (variant == null)
                    throw new InvalidOperationException($"Product variant {item.ProductVariantId} no longer exists.");

                if (variant.Quantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{item.ProductName}'. Only {variant.Quantity} available, but {item.Quantity} needed.");

                variant.Quantity -= item.Quantity;
                _logger.LogInformation("Re-reserved stock for variant {VariantId}: -{Qty} (retry)",
                    variant.Id, item.Quantity);
            }

            // Restore order to PendingPayment
            order.Status = OrderStatus.PendingPayment;

            var amountCents = payment.AmountCents;
            var (paymobOrderId, paymentKey) = await CallPaymobFlowAsync(payment, order, amountCents);

            var attemptNo = payment.Attempts.Any() ? payment.Attempts.Max(a => a.AttemptNo) + 1 : 1;

            var attempt = new PaymentAttempt
            {
                PaymentId = payment.Id,
                AttemptNo = attemptNo,
                Status = PaymentStatus.RequiresAction,
                ProviderOrderId = paymobOrderId.ToString(),
                ProviderPaymentKey = paymentKey,
                CreatedAt = DateTime.UtcNow
            };

            _db.PaymentAttempts.Add(attempt);

            payment.Status = PaymentStatus.RequiresAction;
            payment.ProviderOrderId = paymobOrderId.ToString();
            payment.ErrorCode = null;
            payment.ErrorMessage = null;
            payment.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Payment retry: PaymentId={PaymentId}, AttemptNo={AttemptNo}, PaymobOrder={PaymobOrderId}",
                payment.Id, attemptNo, paymobOrderId);

            return new PaymobSessionResponseDto
            {
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                IframeUrl = _client.BuildIframeUrl(paymentKey),
                IdempotencyKey = payment.IdempotencyKey
            };
        }

        // ═══════════════════════ Private Helpers ═══════════════════════

        private async Task<(long PaymobOrderId, string PaymentKey)> CallPaymobFlowAsync(
            Payment payment, Order order, long amountCents)
        {
            // 1. Auth token
            var authToken = await _client.GetAuthTokenAsync();

            // 2. Register Paymob order
            var merchantOrderId = $"order-{order.Id}-pay-{payment.Id}";
            var paymobOrderId = await _client.RegisterOrderAsync(authToken, amountCents, merchantOrderId, order);

            // 3. Payment key
            var paymentKey = await _client.RequestPaymentKeyAsync(authToken, amountCents, paymobOrderId, order);

            return (paymobOrderId, paymentKey);
        }
    }
}
