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
            // Use the EXACT claim type from the token
            var userId = User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                          ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub")
                          ?? User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var id))
            {
                // Debug: log all claims
                var allClaims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
                Console.WriteLine($"❌ Cannot find user ID. Available claims: {allClaims}");
                throw new UnauthorizedAccessException("Cannot extract user ID from token.");
            }

            Console.WriteLine($"✅ Found user ID: {id}");
            return id;
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
                user.ProfileImage,
                Role = user.Role.ToString()
            });
        }

        public class UpdateProfileDto
        {
            public string? Name { get; set; }
            public string? ProfileImage { get; set; }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data.");

            var userId = GetUserId();
            var user = await _uow.Users.GetByIdAsync(userId);

            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Name))
                user.Name = dto.Name.Trim();
            
            if (!string.IsNullOrWhiteSpace(dto.ProfileImage))
                user.ProfileImage = dto.ProfileImage;

            user.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully", profileImage = user.ProfileImage });
        }

        // ========================= Upload Profile Image =========================
        [HttpPost("profile/upload-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Only image files are allowed (jpg, jpeg, png, gif)");

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File size must not exceed 5MB");

            try
            {
                // Convert to base64
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    var base64String = Convert.ToBase64String(fileBytes);
                    var dataUrl = $"data:{file.ContentType};base64,{base64String}";

                    var userId = GetUserId();
                    var user = await _uow.Users.GetByIdAsync(userId);

                    if (user == null)
                        return NotFound("User not found");

                    user.ProfileImage = dataUrl;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _uow.SaveChangesAsync();

                    return Ok(new { message = "Image uploaded successfully", profileImage = dataUrl });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to upload image", details = ex.Message });
            }
        }

        // ========================= Cart =========================
        // Update the UserController GetCart() method - line 152
        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await _uow.Carts.GetByUserIdAsync(userId);

            if (cart == null || cart.Items == null || !cart.Items.Any())
                return Ok(new
                {
                    items = new List<object>(),
                    totalPrice = 0m,
                    totalQuantity = 0
                });

            return Ok(new
            {
                items = cart.Items.Select(i => new
                {
                    productVariantId = i.ProductVariantId,
                    productName = i.ProductName,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice
                }),
                totalPrice = cart.Items.Sum(i => i.UnitPrice * i.Quantity),
                totalQuantity = cart.Items.Sum(i => i.Quantity)
            });
        }

        // ========================= Orders =========================
        // Correct implementation - make sure GetMyOrders() handles null safely
        [HttpGet("orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userId = GetUserId();
                var orders = await _uow.Orders.GetOrderByUserId(userId);

                if (orders == null || orders.Count == 0)
                    return Ok(new { items = new List<OrderItemDto>(), totalPrice = 0m });

                var items = new List<OrderItemDto>();
                decimal totalPrice = 0m;

                foreach (var order in orders)
                {
                    if (order.Items != null)
                    {
                        foreach (var item in order.Items)
                        {
                            var orderItemDto = new OrderItemDto
                            {
                                ProductVariantId = item.ProductVariantId,
                                ProductName = item.ProductName ?? string.Empty,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitePrice
                            };
                            items.Add(orderItemDto);
                            totalPrice += item.UnitePrice * item.Quantity;
                        }
                    }
                }

                return Ok(new { items = items, totalPrice = totalPrice });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching orders", error = ex.Message });
            }
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
                OrderId = order.Id,
                PaymentUrl = paymentUrl
            });
        }
    }
}
