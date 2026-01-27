using E_Commerce.Dtos.CartDto;
using E_Commerce.Dtos.UserDto;
using E_Commerce.Services.CartService;
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

        // ===== Helper: try to get userId from JWT (sub) =====
        // Returns null when extraction/parsing fails instead of throwing, so callers can return 401.
        private int? GetUserId()
        {
            // Try all possible claim names
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                          ?? User.FindFirstValue("sub")
                          ?? User.FindFirstValue("id");

            if (string.IsNullOrEmpty(userIdStr))
            {
                return null;
            }

            if (!int.TryParse(userIdStr, out var id))
            {
                return null;
            }

            return id;
        }

        // ========================= Get My Cart =========================
        // GET /api/cart
        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized("Invalid or missing user token.");
            var cartDto = await _cartService.GetUserCart(userId.Value);

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
            if (!userId.HasValue) return Unauthorized("Invalid or missing user token.");
            try
            {
                await _cartService.AddToCart(userId.Value, item);
            }
            catch (InvalidOperationException inv)
            {
                return BadRequest(new { message = inv.Message });
            }

            var cartDto = await _cartService.GetUserCart(userId.Value);
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
            if (!userId.HasValue) return Unauthorized("Invalid or missing user token.");
            await _cartService.RemoveFromCart(userId.Value, item);

            var cartDto = await _cartService.GetUserCart(userId.Value);
            return Ok(cartDto);
        }

        // ========================= Increase Quantity =========================
        // POST /api/cart/items/increase
        [HttpPost("items/increase")]
        public async Task<IActionResult> IncreaseQuantity([FromBody] CartItemDto item)
        {
            if (item == null)
                return BadRequest("Item cannot be null.");

            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized("Invalid or missing user token.");

            // Add one more of the same item
            item.Quantity = 1;
            await _cartService.AddToCart(userId.Value, item);

            var cartDto = await _cartService.GetUserCart(userId.Value);
            return Ok(cartDto);
        }

        // ========================= Decrease Quantity =========================
        // POST /api/cart/items/decrease
        [HttpPost("items/decrease")]
        public async Task<IActionResult> DecreaseQuantity([FromBody] CartItemDto item)
        {
            if (item == null)
                return BadRequest("Item cannot be null.");

            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized("Invalid or missing user token.");

            // Remove one of the same item
            item.Quantity = 1;
            await _cartService.RemoveFromCart(userId.Value, item);

            var cartDto = await _cartService.GetUserCart(userId.Value);
            return Ok(cartDto);
        }

        // ========================= Clear Cart =========================
        // DELETE /api/cart/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> Clear()
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized("Invalid or missing user token.");
            await _cartService.ClearCart(userId.Value);

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
            if (!userId.HasValue) return Unauthorized("Invalid or missing user token.");
            var orderId = await _cartService.CheckOutAsync(userId.Value, dto);

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
            if (!userId.HasValue) return Unauthorized("Invalid or missing user token.");
            await _cartService.FromGuestCartToUserCart(userId.Value, cart);

            var cartDto = await _cartService.GetUserCart(userId.Value);
            return Ok(cartDto);
        }
    }
}
