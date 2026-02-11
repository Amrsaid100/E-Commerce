using E_Commerce.Services.PayMob;
using Microsoft.Extensions.Options;

namespace E_Commerce.Helpers
{
    /// <summary>
    /// Validates Paymob configuration at startup.
    /// In Production, missing credentials cause a hard failure (app won't boot).
    /// In Development, missing credentials log a loud warning.
    /// </summary>
    public static class PaymobConfigValidator
    {
        public static void Validate(IServiceProvider services, IWebHostEnvironment env, ILogger logger)
        {
            var settings = services.GetRequiredService<IOptions<PaymobSettings>>().Value;
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(settings.ApiKey) || settings.ApiKey.StartsWith("YOUR_"))
                errors.Add("PAYMOB_API_KEY (Paymob:ApiKey) is not configured.");

            if (string.IsNullOrWhiteSpace(settings.IntegrationId) || settings.IntegrationId.StartsWith("YOUR_"))
                errors.Add("PAYMOB_INTEGRATION_ID_CARD (Paymob:IntegrationId) is not configured.");

            if (string.IsNullOrWhiteSpace(settings.IframeId) || settings.IframeId.StartsWith("YOUR_"))
                errors.Add("PAYMOB_IFRAME_ID_CARD (Paymob:IframeId) is not configured.");

            if (string.IsNullOrWhiteSpace(settings.HmacSecret) || settings.HmacSecret.StartsWith("YOUR_"))
                errors.Add("PAYMOB_HMAC_SECRET (Paymob:HmacSecret) is not configured.");

            if (string.IsNullOrWhiteSpace(settings.BaseUrl))
                errors.Add("PAYMOB_BASE_URL (Paymob:BaseUrl) is not configured.");

            if (errors.Count == 0)
            {
                logger.LogInformation("✅ Paymob configuration validated successfully.");
                return;
            }

            var message = "Paymob configuration errors:\n" + string.Join("\n  - ", errors);

            if (env.IsProduction())
            {
                // HARD FAILURE in production — app must not start without payment credentials
                logger.LogCritical("❌ FATAL: {Message}", message);
                throw new InvalidOperationException(
                    $"Application startup aborted: {message}\n" +
                    "Set all required Paymob environment variables before deploying to production.");
            }
            else
            {
                // In development, log a loud warning but allow startup for non-payment work
                logger.LogWarning("⚠️ {Message}\nPayment features will fail until these are configured.", message);
            }
        }
    }
}
