using E_Commerce.Dtos.UserDto;
using E_Commerce.Services.PayMob;
using E_Commerce.Services.CartService;
using E_Commerce.Services.EmailService;
using E_Commerce.UnitOfWork;
using E_Commerce.DataContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CheckOutController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IPaymobPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _work;
        private readonly EcommerceDbContext _db;
        private readonly ILogger<CheckOutController> _logger;

        public CheckOutController(
            ICartService cartService,
            IPaymobPaymentService paymentService,
            IEmailService emailService,
            IUnitOfWork work,
            EcommerceDbContext db,
            ILogger<CheckOutController> logger)
        {
            _cartService = cartService;
            _paymentService = paymentService;
            _emailService = emailService;
            _work = work;
            _db = db;
            _logger = logger;
        }

        private int GetUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
            {
                _logger.LogWarning("Invalid token: missing/invalid sub");
                throw new UnauthorizedAccessException("Invalid token: missing/invalid sub.");
            }
            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] CheckOutDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid checkout data");

            try
            {
                var userId = GetUserId();

                var orderId = await _cartService.CheckOutAsync(userId, dto);
                if (orderId == 0)
                {
                    _logger.LogWarning("Checkout failed for user {UserId}: Cart is empty", userId);
                    return BadRequest("Cart is empty");
                }

                _logger.LogInformation("✅ Order created successfully: #{OrderId}", orderId);

                // Send owner notification email (inline, won't break order flow on failure)
                try
                {
                    _logger.LogInformation("🔍 Starting email notification process for Order #{OrderId}", orderId);
                    
                    // Small delay to ensure transaction is committed
                    await Task.Delay(100);
                    
                    // Force a fresh query with explicit loading to ensure Items are populated
                    var order = await _db.Orders
                        .Include(o => o.Items)
                        .Include(o => o.User)
                        .Include(o => o.Governorate)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == orderId);
                    
                    if (order != null)
                    {
                        var ownerEmail = order.User?.Email ?? "unknown";
                        _logger.LogInformation("📧 Sending owner email for Order #{OrderId} with {ItemCount} items", 
                            orderId, order.Items?.Count ?? 0);
                        await _emailService.SendOwnerNewOrderEmailAsync(order);
                        _logger.LogInformation("✅ Email sent successfully for Order #{OrderId}", orderId);
                    }
                    else
                    {
                        _logger.LogError("❌ Could not load order #{OrderId} for email notification", orderId);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Email notification failed for Order #{OrderId} (order still created)", orderId);
                }

                // Check payment method
                if (dto.PaymentMethod == "CashOnDelivery")
                {
                    // Cash on Delivery - No payment needed upfront
                    _logger.LogInformation("COD checkout successful for user {UserId}, order {OrderId}", userId, orderId);
                    
                    return Ok(new
                    {
                        orderId,
                        paymentMethod = "CashOnDelivery",
                        message = "Order placed successfully. Payment will be collected on delivery."
                    });
                }
                else
                {
                    // Paymob online payment - Create payment session
                    var idempotencyKey = Request.Headers.TryGetValue("Idempotency-Key", out var headerKey)
                        ? headerKey.ToString()
                        : null;

                    var session = await _paymentService.CreateSessionAsync(orderId, userId, idempotencyKey);

                    _logger.LogInformation("Paymob checkout successful for user {UserId}, order {OrderId}, payment {PaymentId}",
                        userId, orderId, session.PaymentId);

                    return Ok(new
                    {
                        orderId,
                        paymentId = session.PaymentId,
                        iframeUrl = session.IframeUrl,
                        idempotencyKey = session.IdempotencyKey,
                        paymentMethod = "Paymob"
                    });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized checkout attempt: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Checkout validation failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                return StatusCode(500, new { message = "Checkout failed. Please try again." });
            }
        }
    }
}
