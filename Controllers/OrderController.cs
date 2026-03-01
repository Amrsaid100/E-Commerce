using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
using E_Commerce.Services.CartService;
using E_Commerce.Services.EmailService;
using E_Commerce.Services.PayMob;
using E_Commerce.DataContext;
using E_Commerce.Dtos.CartDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace E_Commerce.Controllers
{
    [Route("api/orders")]
    [ApiController]

    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitofwork;
        private readonly ICartService _cartService;
        private readonly IPaymobPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly EcommerceDbContext _db;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IUnitOfWork unitofwork, ICartService cartService, IPaymobPaymentService paymentService, IEmailService emailService, EcommerceDbContext db, ILogger<OrderController> logger)
        {
            _unitofwork = unitofwork;
            _cartService = cartService;
            _paymentService = paymentService;
            _emailService = emailService;
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _unitofwork.Orders.GetAllAsync();
            foreach (var order in orders)
            {
                order.Items = order.Items ?? new List<OrderItem>();
                if (order.User == null)
                {
                    order.User = await _unitofwork.Users.GetByIdAsync(order.UserId);
                }
            }
            var dtos = orders.Select(E_Commerce.Repository.OrderRepo.ToUserOrderDto).ToList();
            return Ok(dtos);
        }

        [HttpGet("user/{userId:int}")]
        [Authorize]
        public async Task<IActionResult> GetOrdersByUserId(int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid user ID");

            var orders = await _unitofwork.Orders.GetOrderByUserId(userId);
            if (orders == null || !orders.Any())
                return NotFound("No orders found for this user");
            foreach (var order in orders)
            {
                if (order.User == null)
                {
                    order.User = await _unitofwork.Users.GetByIdAsync(order.UserId);
                }
            }
            var dtos = orders.Select(E_Commerce.Repository.OrderRepo.ToUserOrderDto).ToList();
            return Ok(dtos);
        }

        [HttpPut("status/{orderId:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID" });

            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { message = "Status is required" });

            var order = await _unitofwork.Orders.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            // Parse string status to enum
            if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
                return BadRequest(new { message = $"Invalid status value: {request.Status}" });

            // ═══════════════ SAFETY GUARD ═══════════════
            // CRITICAL: Admins CANNOT set Order to Paid via this endpoint.
            // Order=Paid can ONLY happen through a verified Paymob webhook.
            // Use the admin payment override endpoint for exceptional cases.
            if (status == OrderStatus.Paid)
            {
                return BadRequest(new { message = "Cannot set order to Paid manually. Orders are marked Paid only by verified payment webhook. Use /api/admin/payments/{id}/override-status for exceptional cases." });
            }

            // If transitioning to Failed/Cancelled, restore inventory
            if ((status == OrderStatus.Failed || status == OrderStatus.Cancelled) &&
                order.Status != OrderStatus.Failed && order.Status != OrderStatus.Cancelled)
            {
                var orderWithItems = await _db.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (orderWithItems != null)
                {
                    foreach (var item in orderWithItems.Items)
                    {
                        var variant = await _unitofwork.ProductVariants.GetByIdAsync(item.ProductVariantId);
                        if (variant != null)
                            variant.Quantity += item.Quantity;
                    }
                }
            }

            order.Status = status;
            await _unitofwork.SaveChangesAsync();

            return Ok(new { message = "Order status updated successfully", orderId = order.Id, status = order.Status.ToString() });
        }

        [HttpDelete("{orderId:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID" });

            var order = await _unitofwork.Orders.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            await _unitofwork.Orders.DeleteAsync(order);
            await _unitofwork.SaveChangesAsync();

            return Ok(new { message = "Order deleted successfully", orderId = orderId });
        }

        [HttpPut("cancel/{orderId:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID" });

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound(new { message = "Order not found" });

            if (order.Status == OrderStatus.Cancelled)
                return BadRequest(new { message = "Order is already cancelled" });

            // Restore inventory for all order items
            foreach (var item in order.Items)
            {
                var variant = await _unitofwork.ProductVariants.GetByIdAsync(item.ProductVariantId);
                if (variant != null)
                    variant.Quantity += item.Quantity;
            }

            order.Status = OrderStatus.Cancelled;

            // Also sync associated payment status to Canceled
            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId &&
                    p.Status != PaymentStatus.Canceled &&
                    p.Status != PaymentStatus.Failed &&
                    p.Status != PaymentStatus.Refunded);

            if (payment != null)
            {
                payment.Status = PaymentStatus.Canceled;
                payment.ErrorMessage = "Order canceled by admin";
                payment.UpdatedAt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Egypt Standard Time");
            }

            await _unitofwork.SaveChangesAsync();

            return Ok(new { message = "Order cancelled successfully. Inventory restored.", orderId = order.Id, status = order.Status.ToString() });
        }

        // ========================= Buy Now Endpoint =========================
        [HttpPost("buy-now")]
        [Authorize]
        public async Task<IActionResult> BuyNow([FromBody] BuyNowDto buyNowDto)
        {
            try
            {
                _logger.LogInformation("🔍 BuyNow called with data: {BuyNowData}", System.Text.Json.JsonSerializer.Serialize(buyNowDto));

                if (buyNowDto == null)
                {
                    _logger.LogWarning("❌ BuyNowDto is null");
                    return BadRequest("Invalid buy-now data");
                }

                if (buyNowDto.Item == null)
                {
                    _logger.LogWarning("❌ BuyNowDto.Item is null");
                    return BadRequest("Invalid item data");
                }

                var userId = GetUserId();
                _logger.LogInformation("🔍 Processing Buy Now for UserId: {UserId}", userId);

                var orderId = await _cartService.BuyNowAsync(userId, buyNowDto);
                if (orderId == 0)
                {
                    _logger.LogWarning("❌ BuyNowAsync returned 0 for UserId: {UserId}", userId);
                    return BadRequest("Buy Now order creation failed");
                }

                _logger.LogInformation("✅ Buy Now order created successfully: #{OrderId}", orderId);
                
                // Send owner notification email (inline, won't break order flow on failure)
                try
                {
                    _logger.LogInformation("🔍 Starting email notification process for Buy Now Order #{OrderId}", orderId);
                    
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
                        _logger.LogInformation("📧 Sending owner email for Buy Now Order #{OrderId} with {ItemCount} items", 
                            orderId, order.Items?.Count ?? 0);
                        await _emailService.SendOwnerNewOrderEmailAsync(order);
                        _logger.LogInformation("✅ Email sent successfully for Buy Now Order #{OrderId}", orderId);
                    }
                    else
                    {
                        _logger.LogError("❌ Could not load Buy Now order #{OrderId} for email notification", orderId);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Email notification failed for Buy Now Order #{OrderId} (order still created)", orderId);
                }

                // Check payment method
                if (buyNowDto.PaymentMethod == "CashOnDelivery")
                {
                    return Ok(new { orderId, paymentMethod = "CashOnDelivery", message = "Order placed successfully. Payment will be collected on delivery." });
                }
                else
                {
                    // Paymob online payment
                    var idempotencyKey = Request.Headers.TryGetValue("Idempotency-Key", out var headerKey)
                        ? headerKey.ToString()
                        : null;
                    
                    var session = await _paymentService.CreateSessionAsync(orderId, userId, idempotencyKey);
                    
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
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "❌ InvalidOperationException in BuyNow: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in BuyNow: {Message}", ex.Message);
                return StatusCode(500, new { message = "Error processing Buy Now order", error = ex.Message });
            }
        }

        // Helper method to extract user ID from JWT token
        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                          ?? User.FindFirstValue("sub")
                          ?? User.FindFirstValue("id");
            
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid token: missing/invalid user ID.");
            }
            return userId;
        }
    }

    // Helper class for status update request
    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}