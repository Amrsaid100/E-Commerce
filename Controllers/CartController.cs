using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using E_Commerce.Services.UserService;
using E_Commerce.Dtos.CartDto;
namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IUnitOfWork _Uow;
        public CartController(ICartService cartService, IUnitOfWork Uow)
        {
            _cartService = cartService;
            _Uow = Uow;
        }

        [HttpGet]
        public async Task<IActionResult> GetCartByUserId(int userId)
        {
            var cart = await _Uow.Carts.GetByUserIdAsync(userId);
            if (cart == null)
            {
                return NotFound("Cart not found for the specified user.");
            }
            return Ok(cart);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(int userId, CartItemDto item)
        {
            if (item == null)
            {
                return BadRequest("Item cannot be null.");
            }
            await _cartService.AddToCart(userId, item);
            return Ok("Item added to cart successfully.");
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromCart(int userId, CartItemDto item)
        {
            if (item == null)
            {
                return BadRequest("Item cannot be null.");
            }
            await _cartService.RemoveFromCart(userId, item);
            return Ok("Item removed from cart successfully.");
        }

        [HttpPost("guest-to-user")]
        public async Task<IActionResult> FromGuestToUser(int UserId,CartDto cart)
        {
            if (cart == null)
                return BadRequest("This car can not be null");

            await _cartService.FromGuestCartToUserCart(UserId,cart);
            return Ok("Converting From Guest To User");
        }
    }
}
