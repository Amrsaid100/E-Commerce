using E_Commerce.Dtos.UserDto;
using E_Commerce.Services.PaymentService;
using E_Commerce.Services.CartService;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CheckOutController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _work;

    public CheckOutController(ICartService cartService, IPaymentService paymentService, IUnitOfWork work)
    {
        _cartService = cartService;
        _paymentService = paymentService;
        _work = work;
    }

    private int GetUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Invalid token: missing/invalid sub.");
        return userId;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckOutDto dto)
    {
        var userId = GetUserId();

        var orderId = await _cartService.CheckOutAsync(userId, dto);
        if (orderId == 0) return BadRequest("Cart is empty");

        var paymentUrl = await _paymentService.CreatePaymentUrl(orderId);
        return Ok(new { orderId, paymentUrl });
    }
}
