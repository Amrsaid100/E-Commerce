using E_Commerce.Entities;

namespace E_Commerce.Helpers
{
    /// <summary>
    /// Enforces legal payment status transitions.
    /// 
    /// Legal transitions:
    ///   CREATED → REQUIRES_ACTION
    ///   REQUIRES_ACTION → PROCESSING, SUCCEEDED, FAILED, CANCELED
    ///   PROCESSING → SUCCEEDED, FAILED, CANCELED
    ///   FAILED → REQUIRES_ACTION  (retry)
    ///   CANCELED → REQUIRES_ACTION  (retry)
    ///   SUCCEEDED → REFUNDED, REFUND_REQUESTED
    ///   REFUND_REQUESTED → REFUNDED
    ///   
    /// No transition back to CREATED.
    /// No direct jump to SUCCEEDED from CREATED.
    /// Client requests can never set SUCCEEDED — only webhook can.
    /// </summary>
    public static class PaymentStateMachine
    {
        private static readonly Dictionary<PaymentStatus, HashSet<PaymentStatus>> AllowedTransitions = new()
        {
            [PaymentStatus.Created] = new()
            {
                PaymentStatus.RequiresAction,
                PaymentStatus.Failed,     // Paymob API call failed during session creation
                PaymentStatus.Canceled
            },
            [PaymentStatus.RequiresAction] = new()
            {
                PaymentStatus.Processing,
                PaymentStatus.Succeeded,  // Webhook: user completed payment
                PaymentStatus.Failed,     // Webhook: payment declined
                PaymentStatus.Canceled    // Webhook: payment voided
            },
            [PaymentStatus.Processing] = new()
            {
                PaymentStatus.Succeeded,
                PaymentStatus.Failed,
                PaymentStatus.Canceled
            },
            [PaymentStatus.Failed] = new()
            {
                PaymentStatus.RequiresAction  // Retry
            },
            [PaymentStatus.Canceled] = new()
            {
                PaymentStatus.RequiresAction  // Retry
            },
            [PaymentStatus.Succeeded] = new()
            {
                PaymentStatus.Refunded,
                PaymentStatus.RefundRequested
            },
            [PaymentStatus.RefundRequested] = new()
            {
                PaymentStatus.Refunded
            },
            [PaymentStatus.Refunded] = new()  // Terminal — no further transitions
            {
            }
        };

        /// <summary>
        /// Check if a transition from current to next status is legal.
        /// </summary>
        public static bool CanTransition(PaymentStatus current, PaymentStatus next)
        {
            if (current == next) return true; // Idempotent: same status is always OK

            return AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
        }

        /// <summary>
        /// Attempt a transition. Throws InvalidOperationException if illegal.
        /// </summary>
        public static void EnsureTransition(PaymentStatus current, PaymentStatus next, int paymentId)
        {
            if (current == next) return;

            if (!CanTransition(current, next))
            {
                throw new InvalidOperationException(
                    $"Illegal payment state transition for Payment #{paymentId}: " +
                    $"{current} → {next}. This transition is not allowed.");
            }
        }

        /// <summary>
        /// Returns true if the status is terminal (no further transitions possible, except refund from Succeeded).
        /// </summary>
        public static bool IsTerminal(PaymentStatus status)
        {
            return status == PaymentStatus.Refunded;
        }

        /// <summary>
        /// Returns true if the payment result has been finalized (succeeded, failed, canceled, refunded).
        /// </summary>
        public static bool IsFinalized(PaymentStatus status)
        {
            return status is PaymentStatus.Succeeded
                or PaymentStatus.Failed
                or PaymentStatus.Canceled
                or PaymentStatus.Refunded;
        }
    }
}
