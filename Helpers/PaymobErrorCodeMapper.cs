namespace E_Commerce.Helpers
{
    /// <summary>
    /// Maps Paymob / internal error codes to user-friendly messages (Arabic + English).
    /// The frontend should prefer displayReason for user-facing text.
    /// </summary>
    public static class PaymobErrorCodeMapper
    {
        /// <summary>
        /// Display status enum for the frontend — controls UI state machine.
        /// The frontend must ONLY show "Order confirmed" when this is SUCCEEDED.
        /// </summary>
        public enum DisplayStatus
        {
            /// <summary>Payment initiated, waiting for user action in iframe</summary>
            AWAITING_PAYMENT,

            /// <summary>Iframe completed, waiting for Paymob webhook confirmation</summary>
            WAITING_FOR_CONFIRMATION,

            /// <summary>Webhook arrived with success=true AND amount/currency verified</summary>
            SUCCEEDED,

            /// <summary>Webhook arrived with failure OR amount mismatch</summary>
            FAILED,

            /// <summary>Payment was voided/canceled</summary>
            CANCELED,

            /// <summary>No webhook arrived within timeout window</summary>
            TIMEOUT,

            /// <summary>Refund requested or completed</summary>
            REFUNDED
        }

        /// <summary>
        /// Convert internal PaymentStatus + error info into a DisplayStatus for the frontend.
        /// This is the ONLY source of truth for UI state.
        /// </summary>
        public static DisplayStatus ToDisplayStatus(string paymentStatus)
        {
            return paymentStatus switch
            {
                "Created" => DisplayStatus.AWAITING_PAYMENT,
                "RequiresAction" => DisplayStatus.AWAITING_PAYMENT,
                "Processing" => DisplayStatus.WAITING_FOR_CONFIRMATION,
                "Succeeded" => DisplayStatus.SUCCEEDED,
                "Failed" => DisplayStatus.FAILED,
                "Canceled" => DisplayStatus.CANCELED,
                "Refunded" => DisplayStatus.REFUNDED,
                "RefundRequested" => DisplayStatus.REFUNDED,
                _ => DisplayStatus.WAITING_FOR_CONFIRMATION
            };
        }

        /// <summary>
        /// Map Paymob txn_response_code / internal error code to a safe, user-facing reason.
        /// Returns (arabicMessage, englishMessage).
        /// NEVER expose raw error details or stack traces to the client.
        /// </summary>
        public static (string Arabic, string English) GetUserFriendlyMessage(string? errorCode, string? errorMessage)
        {
            // Normalize error code
            var code = (errorCode ?? "").Trim().ToUpperInvariant();
            var msg = (errorMessage ?? "").ToLowerInvariant();

            // Map known Paymob response codes
            return code switch
            {
                // Insufficient funds
                "INSUFFICIENT_FUNDS" or "51" =>
                    ("رصيد غير كافي أو البنك رفض العملية", "Insufficient funds or bank declined the transaction"),

                // Card declined (generic)
                "DECLINED" or "05" or "14" or "57" =>
                    ("تم رفض العملية من البنك", "Transaction declined by your bank"),

                // Invalid card data
                "INVALID_CARD" or "54" or "56" =>
                    ("بيانات الكارت غير صحيحة", "Invalid card details"),

                // 3DS / auth failure
                "3DS_FAILED" or "AUTH_REQUIRED" =>
                    ("فشل تأكيد العملية (3DS)", "3D Secure verification failed"),

                // Expired card
                "EXPIRED_CARD" or "33" or "36" =>
                    ("صلاحية الكارت منتهية", "Card has expired"),

                // Timeout
                "TIMEOUT" =>
                    ("لم يصل تأكيد الدفع بعد. لو تم الخصم، هيتأكد تلقائيًا أو هيرجع خلال وقت البنك",
                     "Payment confirmation not received yet. If charged, it will be confirmed automatically or refunded within bank processing time"),

                // User canceled
                "USER_CANCELED" or "VOIDED" =>
                    ("تم إلغاء العملية", "Payment was canceled"),

                // Amount mismatch (our internal guard)
                "AMOUNT_MISMATCH" =>
                    ("خطأ في مبلغ الدفع. تواصل مع خدمة العملاء", "Payment amount mismatch. Please contact support"),

                // Currency mismatch
                "CURRENCY_MISMATCH" =>
                    ("عملة الدفع غير متوافقة", "Payment currency mismatch"),

                // Catch-all: try to detect from message text
                _ when msg.Contains("insufficient") =>
                    ("رصيد غير كافي أو البنك رفض العملية", "Insufficient funds"),
                _ when msg.Contains("declined") || msg.Contains("reject") =>
                    ("تم رفض العملية من البنك", "Transaction declined"),
                _ when msg.Contains("expired") =>
                    ("صلاحية الكارت منتهية", "Card expired"),
                _ when msg.Contains("3d") || msg.Contains("secure") =>
                    ("فشل تأكيد العملية (3DS)", "3D Secure failed"),
                _ when msg.Contains("cancel") || msg.Contains("void") =>
                    ("تم إلغاء العملية", "Payment canceled"),

                // Generic fallback
                _ => ("العملية لم تتم. جرّب مرة تانية", "Payment was not completed. Please try again")
            };
        }

        /// <summary>
        /// Build a safe display reason for the frontend. Never includes internal details.
        /// </summary>
        public static string GetSafeDisplayReason(string? errorCode, string? errorMessage, bool preferArabic = true)
        {
            var (arabic, english) = GetUserFriendlyMessage(errorCode, errorMessage);
            return preferArabic ? arabic : english;
        }
    }
}
