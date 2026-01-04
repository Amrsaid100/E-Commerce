using E_Commerce.Dtos.UserDto;
using E_Commerce.Services.PaymentService;
using E_Commerce.Services.CartService;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _work;
        private readonly ILogger<CheckOutController> _logger;

        public CheckOutController(
            ICartService cartService,
            IPaymentService paymentService,
            IUnitOfWork work,
            ILogger<CheckOutController> logger)
        {
            _cartService = cartService;
            _paymentService = paymentService;
            _work = work;
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
                    _logger.LogWarning($"Checkout failed for user {userId}: Cart is empty");
                    return BadRequest("Cart is empty");
                }

                var paymentUrl = await _paymentService.CreatePaymentUrl(orderId);
                if (string.IsNullOrWhiteSpace(paymentUrl))
                {
                    _logger.LogError($"Failed to create payment URL for order {orderId}");
                    return StatusCode(500, "Failed to create payment URL");
                }

                _logger.LogInformation($"Checkout successful for user {userId}, order {orderId}");
                return Ok(new { orderId, paymentUrl });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Unauthorized checkout attempt: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during checkout: {ex.Message}");
                return StatusCode(500, new { message = "Checkout failed. Please try again." });
            }
        }
    }
}
