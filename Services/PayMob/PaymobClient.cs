using E_Commerce.Entities;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace E_Commerce.Services.PayMob
{
    /// <summary>
    /// Low-level Paymob Accept API client with retry/back-off.
    /// </summary>
    public class PaymobClient : IPaymobClient
    {
        private readonly HttpClient _http;
        private readonly PaymobSettings _settings;
        private readonly ILogger<PaymobClient> _logger;

        private static readonly int[] RetryDelaysMs = { 500, 1500, 3000 };

        public PaymobClient(HttpClient http, IOptions<PaymobSettings> settings, ILogger<PaymobClient> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        // ─────────────────── Step 1: Auth Token ───────────────────
        public async Task<string> GetAuthTokenAsync()
        {
            var payload = new { api_key = _settings.ApiKey };
            var response = await PostWithRetryAsync($"{_settings.BaseUrl}/auth/tokens", payload);

            using var doc = JsonDocument.Parse(response);
            var token = doc.RootElement.GetProperty("token").GetString();

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Paymob auth returned empty token.");

            _logger.LogInformation("Paymob auth token obtained successfully.");
            return token;
        }

        // ─────────────────── Step 2: Register Order ───────────────────
        public async Task<long> RegisterOrderAsync(string authToken, long amountCents, string merchantOrderId, Order order)
        {
            var payload = new
            {
                auth_token = authToken,
                delivery_needed = false,
                amount_cents = amountCents,
                currency = _settings.Currency,
                merchant_order_id = merchantOrderId,
                items = order.Items.Select(i => new
                {
                    name = i.ProductName ?? $"Item-{i.ProductVariantId}",
                    amount_cents = (long)(i.UnitePrice * 100) * i.Quantity,
                    quantity = i.Quantity,
                    description = i.ProductName ?? ""
                }).ToArray()
            };

            var response = await PostWithRetryAsync($"{_settings.BaseUrl}/ecommerce/orders", payload);

            using var doc = JsonDocument.Parse(response);
            var paymobOrderId = doc.RootElement.GetProperty("id").GetInt64();

            _logger.LogInformation("Paymob order registered: {PaymobOrderId} for merchant order {MerchantOrderId}",
                paymobOrderId, merchantOrderId);

            return paymobOrderId;
        }

        // ─────────────────── Step 3: Payment Key ───────────────────
        public async Task<string> RequestPaymentKeyAsync(string authToken, long amountCents, long paymobOrderId, Order order)
        {
            var payload = new
            {
                auth_token = authToken,
                amount_cents = amountCents,
                expiration = 3600,
                order_id = paymobOrderId,
                currency = _settings.Currency,
                integration_id = int.Parse(_settings.IntegrationId),
                billing_data = new
                {
                    email = order.Email ?? "customer@example.com",
                    first_name = "Customer",
                    last_name = "User",
                    phone_number = order.PhoneNumber ?? "01000000000",
                    street = order.Street ?? "N/A",
                    building = "N/A",
                    floor = "N/A",
                    apartment = "N/A",
                    city = order.Neighborhood ?? "Cairo",
                    state = order.Neighborhood ?? "Cairo",
                    country = "EG",
                    postal_code = "00000",
                    shipping_method = "N/A"
                }
            };

            var response = await PostWithRetryAsync($"{_settings.BaseUrl}/acceptance/payment_keys", payload);

            using var doc = JsonDocument.Parse(response);
            var paymentKey = doc.RootElement.GetProperty("token").GetString();

            if (string.IsNullOrWhiteSpace(paymentKey))
                throw new InvalidOperationException("Paymob returned empty payment key.");

            _logger.LogInformation("Paymob payment key generated for order {PaymobOrderId}", paymobOrderId);
            return paymentKey;
        }

        // ─────────────────── Iframe URL ───────────────────
        public string BuildIframeUrl(string paymentKey)
        {
            return $"{_settings.BaseUrl}/acceptance/iframes/{_settings.IframeId}?payment_token={paymentKey}";
        }

        // ─────────────────── HMAC Verification ───────────────────
        /// <summary>
        /// Verifies the HMAC SHA-512 signature of a Paymob webhook.
        /// Paymob sends the HMAC in the query parameter or header.
        /// The HMAC is computed over specific fields from the transaction obj, concatenated in a specific order.
        /// See: https://docs.paymob.com/docs/transaction-webhooks#hmac-calculation
        /// </summary>
        public bool VerifyHmac(string hmacHeader, string rawBody)
        {
            if (string.IsNullOrWhiteSpace(hmacHeader) || string.IsNullOrWhiteSpace(rawBody))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                // Paymob sends the transaction data in obj
                JsonElement obj;
                if (root.TryGetProperty("obj", out var objProp))
                    obj = objProp;
                else
                    obj = root; // fallback: the root itself is the transaction

                // Extract fields in the EXACT order Paymob specifies for HMAC calculation
                var fields = new List<string>();

                fields.Add(GetJsonValue(obj, "amount_cents"));
                fields.Add(GetJsonValue(obj, "created_at"));
                fields.Add(GetJsonValue(obj, "currency"));
                fields.Add(GetJsonValue(obj, "error_occured"));
                fields.Add(GetJsonValue(obj, "has_parent_transaction"));
                fields.Add(GetJsonValue(obj, "id"));
                fields.Add(GetJsonValue(obj, "integration_id"));
                fields.Add(GetJsonValue(obj, "is_3d_secure"));
                fields.Add(GetJsonValue(obj, "is_auth"));
                fields.Add(GetJsonValue(obj, "is_capture"));
                fields.Add(GetJsonValue(obj, "is_refunded"));
                fields.Add(GetJsonValue(obj, "is_standalone_payment"));
                fields.Add(GetJsonValue(obj, "is_voided"));

                // order.id
                if (obj.TryGetProperty("order", out var orderProp))
                    fields.Add(GetJsonValue(orderProp, "id"));
                else
                    fields.Add("");

                fields.Add(GetJsonValue(obj, "owner"));
                fields.Add(GetJsonValue(obj, "pending"));

                // source_data.pan
                if (obj.TryGetProperty("source_data", out var srcData))
                {
                    fields.Add(GetJsonValue(srcData, "pan"));
                    fields.Add(GetJsonValue(srcData, "sub_type"));
                    fields.Add(GetJsonValue(srcData, "type"));
                }
                else
                {
                    fields.Add(""); fields.Add(""); fields.Add("");
                }

                fields.Add(GetJsonValue(obj, "success"));

                var concatenated = string.Join("", fields);

                using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.HmacSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
                var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                var isValid = computed.Equals(hmacHeader.ToLowerInvariant(), StringComparison.Ordinal);

                if (!isValid)
                    _logger.LogWarning("HMAC verification failed. Expected: {Computed}, Received: {HmacHeader}", computed, hmacHeader);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during HMAC verification");
                return false;
            }
        }

        // ─────────────────── Refund ───────────────────
        public async Task<(bool Success, string Message)> RefundTransactionAsync(string authToken, long transactionId, long amountCents)
        {
            try
            {
                var payload = new
                {
                    auth_token = authToken,
                    transaction_id = transactionId,
                    amount_cents = amountCents
                };

                var response = await PostWithRetryAsync($"{_settings.BaseUrl}/acceptance/void_refund/refund", payload);

                using var doc = JsonDocument.Parse(response);

                // Check if refund was successful
                if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    _logger.LogInformation("Refund successful for transaction {TransactionId}, amount {AmountCents}",
                        transactionId, amountCents);
                    return (true, "Refund processed successfully.");
                }

                var message = doc.RootElement.TryGetProperty("message", out var msgProp)
                    ? msgProp.GetString() ?? "Refund failed"
                    : "Refund failed";

                _logger.LogWarning("Refund failed for transaction {TransactionId}: {Message}", transactionId, message);
                return (false, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund error for transaction {TransactionId}", transactionId);
                return (false, $"Refund error: {ex.Message}");
            }
        }

        // ─────────────────── Helpers ───────────────────

        private static string GetJsonValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return "";

            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString() ?? "",
                JsonValueKind.Number => prop.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "",
                _ => prop.GetRawText()
            };
        }

        private async Task<string> PostWithRetryAsync(string url, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            for (int i = 0; i <= RetryDelaysMs.Length; i++)
            {
                try
                {
                    var response = await _http.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                        return await response.Content.ReadAsStringAsync();

                    var body = await response.Content.ReadAsStringAsync();

                    // 4xx errors are not retryable
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        _logger.LogWarning("Paymob API {StatusCode} for {Url}: {Body}",
                            response.StatusCode, url, body);
                        throw new InvalidOperationException(
                            $"Paymob API error ({response.StatusCode}): {body}");
                    }

                    // 5xx → retry
                    _logger.LogWarning("Paymob API {StatusCode} (attempt {Attempt}) for {Url}",
                        response.StatusCode, i + 1, url);
                }
                catch (HttpRequestException ex) when (i < RetryDelaysMs.Length)
                {
                    _logger.LogWarning(ex, "Paymob HTTP error (attempt {Attempt}) for {Url}", i + 1, url);
                }
                catch (TaskCanceledException ex) when (i < RetryDelaysMs.Length)
                {
                    _logger.LogWarning(ex, "Paymob timeout (attempt {Attempt}) for {Url}", i + 1, url);
                }

                if (i < RetryDelaysMs.Length)
                    await Task.Delay(RetryDelaysMs[i]);
            }

            throw new InvalidOperationException($"Paymob API call failed after {RetryDelaysMs.Length + 1} attempts: {url}");
        }
    }
}
