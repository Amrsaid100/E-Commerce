using System.Text.Json.Serialization;

namespace E_Commerce.Dtos.Payment
{
    /// <summary>
    /// Paymob Accept webhook callback payload (transaction processed callback).
    /// See: https://docs.paymob.com/docs/transaction-webhooks
    /// </summary>
    public class PaymobWebhookCallbackDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("obj")]
        public PaymobTransactionObj? Obj { get; set; }
    }

    public class PaymobTransactionObj
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        [JsonPropertyName("amount_cents")]
        public long AmountCents { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("is_auth")]
        public bool IsAuth { get; set; }

        [JsonPropertyName("is_capture")]
        public bool IsCapture { get; set; }

        [JsonPropertyName("is_standalone_payment")]
        public bool IsStandalonePayment { get; set; }

        [JsonPropertyName("is_voided")]
        public bool IsVoided { get; set; }

        [JsonPropertyName("is_refunded")]
        public bool IsRefunded { get; set; }

        [JsonPropertyName("is_3d_secure")]
        public bool Is3dSecure { get; set; }

        [JsonPropertyName("error_occured")]
        public bool ErrorOccured { get; set; }

        [JsonPropertyName("has_parent_transaction")]
        public bool HasParentTransaction { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("order")]
        public PaymobWebhookOrder? Order { get; set; }

        [JsonPropertyName("data")]
        public PaymobTransactionData? Data { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }

    public class PaymobWebhookOrder
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("merchant_order_id")]
        public string? MerchantOrderId { get; set; }
    }

    public class PaymobTransactionData
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("txn_response_code")]
        public string? TxnResponseCode { get; set; }
    }
}
