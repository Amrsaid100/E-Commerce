using E_Commerce.Entities;

namespace E_Commerce.Services.PayMob
{
    public class PaymobService : IPaymobService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public PaymobService(IConfiguration config, HttpClient http)
        {
            _config = config;
            _http = http;
        }

        public async Task<string> CreatePaymentUrlAsync(Order order)
        {
            // 1️⃣ Get auth token
            var authResponse = await _http.PostAsJsonAsync(
                $"{_config["Paymob:BaseUrl"]}/auth/tokens",
                new { api_key = _config["Paymob:ApiKey"] }
            );
            var authData = await authResponse.Content.ReadFromJsonAsync<dynamic>();
            string token = authData.token;

            // 2️⃣ Create Paymob order
            var orderRequest = new
            {
                auth_token = token,
                delivery_needed = false,
                amount_cents = (int)(order.TotalAmount * 100),
                currency = _config["Paymob:Currency"],
                merchant_order_id = order.OrderId
            };

            var orderResponse = await _http.PostAsJsonAsync(
                $"{_config["Paymob:BaseUrl"]}/ecommerce/orders",
                orderRequest
            );
            var orderData = await orderResponse.Content.ReadFromJsonAsync<dynamic>();
            int paymobOrderId = orderData.id;

            // 3️⃣ Request payment key
            var paymentKeyRequest = new
            {
                auth_token = token,
                amount_cents = (int)(order.TotalAmount * 100),
                expiration = 3600,
                order_id = paymobOrderId,
                billing_data = new
                {
                    email = order.Email,
                    first_name = "User", // optional
                    last_name = "User",
                    phone_number = order.PhoneNumber
                },
                currency = _config["Paymob:Currency"],
                integration_id = _config["Paymob:IntegrationId"]
            };

            var paymentKeyResponse = await _http.PostAsJsonAsync(
                $"{_config["Paymob:BaseUrl"]}/acceptance/payment_keys",
                paymentKeyRequest
            );
            var paymentKeyData = await paymentKeyResponse.Content.ReadFromJsonAsync<dynamic>();
            string paymentToken = paymentKeyData.token;

            // 4️⃣ Return iframe payment URL
            return $"https://accept.paymob.com/api/acceptance/iframes/{_config["Paymob:IframeId"]}?payment_token={paymentToken}";
        }
    }

}
