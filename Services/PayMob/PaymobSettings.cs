namespace E_Commerce.Services.PayMob
{
    public class PaymobSettings
    {
        public string ApiKey { get; set; } = default!;
        public string IntegrationId { get; set; } = default!;
        public string IframeId { get; set; } = default!;
        public string HmacSecret { get; set; } = default!;
        public string Currency { get; set; } = "EGP";
        public string BaseUrl { get; set; } = "https://accept.paymob.com/api";
    }
}
