using E_Commerce.Dtos.Payment;
using E_Commerce.Services.PayMob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace E_Commerce.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobPaymentService _paymentService;
        private readonly IPaymobClient _paymobClient;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymobPaymentService paymentService,
            IPaymobClient paymobClient,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _paymobClient = paymobClient;
            _logger = logger;
        }

        // ═══════════════════════ Create Payment Session ═══════════════════════

        /// <summary>
        /// POST /api/payment/session
        /// Creates a Paymob payment session for an existing order.
        /// The order must be in PendingPayment or Failed state.
        /// Returns: { paymentId, orderId, iframeUrl, idempotencyKey }
        /// </summary>
        [HttpPost("session")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentSession([FromBody] CreatePaymobSessionRequest request)
        {
            try
            {
                var userId = GetUserId();

                // Accept Idempotency-Key from header, fallback to body, fallback to null (server generates)
                var idempotencyKey = Request.Headers.TryGetValue("Idempotency-Key", out var headerKey)
                    ? headerKey.ToString()
                    : request.IdempotencyKey;

                var result = await _paymentService.CreateSessionAsync(request.OrderId, userId, idempotencyKey);

                // Return the idempotency key in response header as well
                Response.Headers.Append("Idempotency-Key", result.IdempotencyKey);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment session");
                return StatusCode(500, new { message = "Failed to create payment session. Please try again." });
            }
        }

        // ═══════════════════════ Get Payment Status ═══════════════════════

        /// <summary>
        /// GET /api/payment/{paymentId}/status
        /// Poll payment status from frontend after iframe interaction.
        /// </summary>
        [HttpGet("{paymentId}/status")]
        [Authorize]
        public async Task<IActionResult> GetPaymentStatus(int paymentId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _paymentService.GetPaymentStatusAsync(paymentId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for {PaymentId}", paymentId);
                return StatusCode(500, new { message = "Failed to get payment status." });
            }
        }

        // ═══════════════════════ Retry Payment ═══════════════════════

        /// <summary>
        /// POST /api/payment/{paymentId}/retry
        /// Creates a new attempt for a failed/canceled payment.
        /// </summary>
        [HttpPost("{paymentId}/retry")]
        [Authorize]
        public async Task<IActionResult> RetryPayment(int paymentId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _paymentService.RetryPaymentAsync(paymentId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying payment {PaymentId}", paymentId);
                return StatusCode(500, new { message = "Failed to retry payment." });
            }
        }

        // ═══════════════════════ Paymob Webhook ═══════════════════════

        /// <summary>
        /// POST /api/payment/webhook
        /// Receives Paymob transaction callbacks. No auth required (from Paymob servers).
        /// HMAC-verified for security.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymobWebhook()
        {
            try
            {
                // 1) Read raw body for HMAC verification
                Request.EnableBuffering();
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();

                _logger.LogInformation("Webhook received. Body length: {Length}", rawBody.Length);

                // 2) Get HMAC from query string (Paymob sends it as ?hmac=...)
                var hmac = Request.Query["hmac"].ToString();
                if (string.IsNullOrWhiteSpace(hmac))
                {
                    // Also check header
                    hmac = Request.Headers["hmac"].ToString();
                }

                if (string.IsNullOrWhiteSpace(hmac))
                {
                    _logger.LogWarning("Webhook received without HMAC");
                    return Unauthorized(new { message = "Missing HMAC" });
                }

                // 3) Verify HMAC
                if (!_paymobClient.VerifyHmac(hmac, rawBody))
                {
                    _logger.LogWarning("Webhook HMAC verification failed");
                    return Unauthorized(new { message = "Invalid HMAC signature" });
                }

                _logger.LogInformation("Webhook HMAC verified successfully");

                // 4) Parse payload
                var callback = System.Text.Json.JsonSerializer.Deserialize<PaymobWebhookCallbackDto>(rawBody,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (callback?.Obj == null)
                {
                    _logger.LogWarning("Webhook payload has no transaction object");
                    return BadRequest(new { message = "Invalid webhook payload" });
                }

                // 5) Process
                await _paymentService.ProcessWebhookAsync(callback.Obj);

                return Ok(new { message = "Webhook processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                // Always return 200 to Paymob so it doesn't retry endlessly
                return Ok(new { message = "Webhook received (error logged)" });
            }
        }

        // ═══════════════════════ Admin Refund ═══════════════════════

        /// <summary>
        /// POST /api/payment/{paymentId}/refund
        /// Admin-only. Refund a succeeded payment.
        /// </summary>
        [HttpPost("{paymentId}/refund")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> Refund(int paymentId, [FromBody] RefundRequestDto request)
        {
            try
            {
                var result = await _paymentService.RefundAsync(paymentId, request.AmountCents, request.Reason);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for {PaymentId}", paymentId);
                return StatusCode(500, new { message = "Refund failed. Please try again." });
            }
        }

        // ═══════════════════════ Helper ═══════════════════════

        private int GetUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid token: missing/invalid sub.");
            return userId;
        }
    }

    // ═══════════════════════ Request Models ═══════════════════════

    public class CreatePaymobSessionRequest
    {
        public int OrderId { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}
