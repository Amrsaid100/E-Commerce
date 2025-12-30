using E_Commerce.Dtos.CartDto;
using E_Commerce.Dtos.UserDto;
using E_Commerce.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace E_Commerce.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // ===== Helper: get userId from JWT (sub) =====
        private int GetUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid token: missing/invalid sub.");

            return userId;
        }

        // ========================= Get My Cart =========================
        // GET /api/cart
        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var userId = GetUserId();
            var cartDto = await _cartService.GetUserCart(userId);

            if (cartDto == null)
                return Ok(new CartDto { Items = new List<CartItemDto>(), TotalPrice = 0m });

            return Ok(cartDto);
        }

        // ========================= Add Item =========================
        // POST /api/cart/items
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] CartItemDto item)
        {
            if (item == null)
                return BadRequest("Item cannot be null.");

            var userId = GetUserId();
            await _cartService.AddToCart(userId, item);

         
            var cartDto = await _cartService.GetUserCart(userId);
            return Ok(cartDto);
        }

        // ========================= Remove Item (decrease/remove) =========================
        // DELETE /api/cart/items
        [HttpDelete("items")]
        public async Task<IActionResult> RemoveItem([FromBody] CartItemDto item)
        {
            if (item == null)
                return BadRequest("Item cannot be null.");

            var userId = GetUserId();
            await _cartService.RemoveFromCart(userId, item);

            var cartDto = await _cartService.GetUserCart(userId);
            return Ok(cartDto);
        }

        // ========================= Clear Cart =========================
        // DELETE /api/cart/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> Clear()
        {
            var userId = GetUserId();
            await _cartService.ClearCart(userId);

            return Ok(new CartDto { Items = new List<CartItemDto>(), TotalPrice = 0m });
        }

        // ========================= Checkout =========================
        // POST /api/cart/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckOutDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid checkout data.");

            var userId = GetUserId();
            var orderId = await _cartService.CheckOutAsync(userId, dto);

            if (orderId == 0)
                return BadRequest("Cart is empty.");

           
            return Ok(new { OrderId = orderId });
        }

        // ========================= Guest -> User =========================
        // POST /api/cart/guest-to-user
        [HttpPost("guest-to-user")]
        public async Task<IActionResult> GuestToUser([FromBody] CartDto cart)
        {
            if (cart == null)
                return BadRequest("Cart cannot be null.");

            var userId = GetUserId();
            await _cartService.FromGuestCartToUserCart(userId, cart);

            var cartDto = await _cartService.GetUserCart(userId);
            return Ok(cartDto);
        }
    }
}
