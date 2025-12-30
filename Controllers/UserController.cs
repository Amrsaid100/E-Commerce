using E_Commerce.Dtos.UserDto;
using E_Commerce.Dtos.OrderDto;
using E_Commerce.Entities;
using E_Commerce.Services.PayMob;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace E_Commerce.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymobService _paymob;

        public UserController(IUnitOfWork uow, IPaymobService paymob)
        {
            _uow = uow;
            _paymob = paymob;
        }

        // ===== Helpers: read from JwtService claims (sub, email) =====
        private int GetUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid token: missing/invalid sub claim.");

            return userId;
        }

        private string GetUserEmail()
        {
            // JwtService uses JwtRegisteredClaimNames.Email
            return User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;
        }

        // ========================= Profile =========================
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var user = await _uow.Users.GetByIdAsync(userId);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Name,
                Role = user.Role.ToString()
            });
        }

        public class UpdateProfileDto
        {
            public string? Name { get; set; }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var userId = GetUserId();
            var user = await _uow.Users.GetByIdAsync(userId);

            if (user == null)
                return NotFound();

            user.Name = dto.Name.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();
            return Ok("Profile updated successfully");
        }

        // ========================= Cart =========================
        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await _uow.Carts.GetByUserIdAsync(userId);

            if (cart == null)
                return Ok(new { Items = new List<object>(), Total = 0m });

            return Ok(new
            {
                cart.Id,
                Items = cart.Items.Select(i => new
                {
                    i.Id,
                    i.ProductVariantId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    Total = i.TotalPrice
                }),
                Total = cart.Items.Sum(i => i.TotalPrice)
            });
        }

        // ========================= Orders =========================
        [HttpGet("orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            var order = await _uow.Orders.GetOrderByUserId(userId);

            if (order == null)
                return Ok(new List<UserOrderDto>());

            var dto = new UserOrderDto
            {
                TotalPrice = order.TotalAmount,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductName ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitePrice
                }).ToList()
            };

            return Ok(dto);
        }

        // ========================= Checkout + Payment =========================
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckOutDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data.");

            var userId = GetUserId();

            // optional: enforce token email == dto email (if u want to ignore anyone to write another email)
            // var tokenEmail = GetUserEmail();
            // if (!string.IsNullOrWhiteSpace(tokenEmail) &&
            //     !string.Equals(tokenEmail, dto.Email, StringComparison.OrdinalIgnoreCase))
            //     return BadRequest("Email must match your account email.");

            var cart = await _uow.Carts.GetByUserIdAsync(userId);

            if (cart == null || cart.Items == null || !cart.Items.Any())
                return BadRequest("Cart is empty");

            var total = cart.Items.Sum(i => i.TotalPrice);

            var order = new Order
            {
                UserId = userId,
                Email = dto.Email,
                City = dto.City,
                Street = dto.Street,
                PhoneNumber = dto.PhoneNumber,
                Status = OrderStatus.PendingPayment,
                TotalAmount = total,
                Items = cart.Items.Select(i => new OrderItem
                {
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitePrice = i.UnitPrice
                }).ToList()
            };

            await _uow.Orders.AddAsync(order);
            await _uow.SaveChangesAsync();

            var paymentUrl = await _paymob.CreatePaymentUrlAsync(order);

            return Ok(new
            {
                OrderId = order.OrderId,
                PaymentUrl = paymentUrl
            });
        }
    }
}
