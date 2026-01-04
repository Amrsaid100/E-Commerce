using E_Commerce.Dtos.Payment;
using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork work;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IUnitOfWork work, IConfiguration config, ILogger<PaymentController> logger)
        {
            this.work = work;
            _config = config;
            _logger = logger;
        }

        [HttpPost("success")]
        public async Task<IActionResult> PaymentSuccess(int orderId, string secret)
        {
            if (secret != _config["Payment:Secret"])
            {
                _logger.LogWarning($"Invalid secret for order {orderId}");
                return Unauthorized("Invalid secret");
            }

            var order = await work.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning($"Order {orderId} not found");
                return NotFound();
            }

            if (order.Status == OrderStatus.Paid)
            {
                _logger.LogInformation($"Order {orderId} already paid");
                return Ok("Payment already processed");
            }

            // Reduce stock
            foreach (var item in order.Items)
            {
                var variant = await work.ProductVariants.GetByIdAsync(item.ProductVariantId);
                if (variant == null)
                    return BadRequest($"Product variant {item.ProductVariantId} not found");

                if (variant.Quantity < item.Quantity)
                    return BadRequest("Out of stock");

                variant.Quantity -= item.Quantity;
            }

            // Mark order paid
            order.Status = OrderStatus.Paid;
            order.PaymentReference = Guid.NewGuid().ToString();

            // Clear cart
            var cart = await work.Carts.GetByUserIdAsync(order.UserId);
            if (cart != null)
                cart.Items.Clear();

            await work.SaveChangesAsync();
            _logger.LogInformation($"Order {orderId} payment successful");
            return Ok("Payment successful");
        }

        [HttpPost("fail")]
        public async Task<IActionResult> PaymentFail(int orderId)
        {
            var order = await work.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning($"Order {orderId} not found");
                return NotFound();
            }

            order.Status = OrderStatus.Failed;
            await work.SaveChangesAsync();

            _logger.LogWarning($"Order {orderId} payment failed");
            return Ok("Payment failed");
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> PaymobWebhook([FromBody] PaymobWebhookDto data)
        {
            if (data == null)
            {
                _logger.LogWarning("Webhook received with null data");
                return BadRequest("Invalid webhook payload");
            }

            try
            {
                int orderId = data.Order?.Id ?? 0;
                bool success = data.Success;

                if (orderId == 0)
                {
                    _logger.LogWarning("Webhook received with invalid order ID");
                    return BadRequest("Invalid order ID");
                }

                var order = await work.Orders.GetByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found in webhook");
                    return NotFound();
                }

                if (success)
                {
                    order.Status = OrderStatus.Paid;
                    order.PaymentReference = data.TransactionId ?? Guid.NewGuid().ToString();

                    // Reduce stock
                    foreach (var item in order.Items)
                    {
                        var variant = await work.ProductVariants.GetByIdAsync(item.ProductVariantId);
                        if (variant != null)
                            variant.Quantity -= item.Quantity;
                    }

                    // Clear cart
                    var cart = await work.Carts.GetByUserIdAsync(order.UserId);
                    if (cart != null)
                        cart.Items.Clear();

                    _logger.LogInformation($"Webhook processed successfully for order {orderId}");
                }
                else
                {
                    order.Status = OrderStatus.Failed;
                    _logger.LogWarning($"Webhook received payment failure for order {orderId}");
                }

                await work.SaveChangesAsync();
                return Ok(new { message = "Webhook processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing webhook: {ex.Message}");
                return StatusCode(500, new { message = "Error processing webhook" });
            }
        }
    }
}
