using E_Commerce.UnitOfWork;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace E_Commerce.Services.PaymentService
{
    public class PaymentSer : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PaymentSer> _logger;

        public PaymentSer(
            IConfiguration config,
            IUnitOfWork uow,
            IHttpClientFactory httpClientFactory,
            ILogger<PaymentSer> logger)
        {
            _config = config;
            _uow = uow;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> CreatePaymentUrl(int orderId)
        {
            try
            {
                var order = await _uow.Orders.GetByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found");
                    return "";
                }

                var paymobApiKey = _config["Paymob:ApiKey"];
                var integrationId = _config["Paymob:IntegrationId"];
                var iframeId = _config["Paymob:IframeId"];
                var baseUrl = _config["Paymob:BaseUrl"];

                if (string.IsNullOrWhiteSpace(paymobApiKey) ||
                    string.IsNullOrWhiteSpace(integrationId) ||
                    string.IsNullOrWhiteSpace(iframeId))
                {
                    _logger.LogError("Paymob API credentials not configured");
                    return "";
                }

                // Create auth token
                var authToken = await GetPaymobAuthToken(paymobApiKey, baseUrl ?? "");
                if (string.IsNullOrWhiteSpace(authToken))
                {
                    _logger.LogError("Failed to get Paymob auth token");
                    return "";
                }

                // Create order in Paymob
                var paymentOrder = await CreatePaymobOrder(
                    authToken,
                    order.TotalAmount,
                    order.Email,
                    order.PhoneNumber,
                    orderId,
                    baseUrl ?? "");

                if (paymentOrder == null)
                {
                    _logger.LogError($"Failed to create Paymob order for order {orderId}");
                    return "";
                }

                // Generate payment URL
                var paymentUrl = $"{baseUrl}/fawry/payments?publicKey={iframeId}&orderReference={paymentOrder}";
                _logger.LogInformation($"Payment URL created for order {orderId}: {paymentUrl}");

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating payment URL: {ex.Message}");
                return "";
            }
        }

        public Task<bool> ValidateWebhookSignature(string payload, string signature)
        {
            try
            {
                var secret = _config["Payment:Secret"];
                if (string.IsNullOrWhiteSpace(secret))
                {
                    _logger.LogWarning("Payment secret not configured");
                    return Task.FromResult(false);
                }

                var hash = ComputeHmacSha256(payload, secret);
                bool isValid = hash.Equals(signature, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogWarning("Webhook signature validation failed");
                }

                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating webhook: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public async Task<(bool Success, string Message)> ProcessWebhookAsync(int orderId, bool success)
        {
            try
            {
                var order = await _uow.Orders.GetByIdAsync(orderId);
                if (order == null)
                    return (false, "Order not found");

                if (success)
                {
                    order.Status = E_Commerce.Entities.OrderStatus.Paid;
                    order.PaymentReference = Guid.NewGuid().ToString();

                    // Reduce stock
                    foreach (var item in order.Items)
                    {
                        var variant = await _uow.ProductVariants.GetByIdAsync(item.ProductVariantId);
                        if (variant == null)
                            return (false, $"Product variant {item.ProductVariantId} not found");

                        if (variant.Quantity < item.Quantity)
                            return (false, "Insufficient stock");

                        variant.Quantity -= item.Quantity;
                    }

                    // Clear cart
                    var cart = await _uow.Carts.GetByUserIdAsync(order.UserId);
                    if (cart != null)
                        cart.Items.Clear();

                    _logger.LogInformation($"Order {orderId} payment successful, stock reduced");
                }
                else
                {
                    order.Status = E_Commerce.Entities.OrderStatus.Failed;
                    _logger.LogWarning($"Order {orderId} payment failed");
                }

                await _uow.SaveChangesAsync();
                return (true, success ? "Payment processed successfully" : "Payment marked as failed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing webhook: {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
        }

        private async Task<string?> GetPaymobAuthToken(string apiKey, string baseUrl)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = new { api_key = apiKey };
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{baseUrl}/auth/tokens", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to get auth token: {response.StatusCode}");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseBody);

                if (jsonDoc.RootElement.TryGetProperty("token", out var tokenElement))
                {
                    return tokenElement.GetString() ?? "";
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting Paymob auth token: {ex.Message}");
                return "";
            }
        }

        private async Task<string?> CreatePaymobOrder(
            string authToken,
            decimal amount,
            string email,
            string phone,
            int orderId,
            string baseUrl)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                var orderData = new
                {
                    auth_token = authToken,
                    delivery_needed = false,
                    currency = "EGP",
                    amount_cents = (long)(amount * 100),
                    items = new[] {
                        new {
                            name = $"Order #{orderId}",
                            amount_cents = (long)(amount * 100),
                            quantity = 1
                        }
                    },
                    customer = new
                    {
                        first_name = "Customer",
                        last_name = "Order",
                        email = email,
                        phone = phone
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(orderData),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{baseUrl}/ecommerce/orders", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to create Paymob order: {response.StatusCode}");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseBody);

                if (jsonDoc.RootElement.TryGetProperty("id", out var idElement))
                {
                    return idElement.GetInt32().ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating Paymob order: {ex.Message}");
                return null;
            }
        }

        private string ComputeHmacSha256(string input, string secret)
        {
            byte[] key = Encoding.UTF8.GetBytes(secret);
            using (var hmac = new HMACSHA256(key))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
