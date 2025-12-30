using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
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

        public PaymentController(IUnitOfWork work, IConfiguration config)
        {
            this.work = work;
            _config = config;
        }

        [HttpPost("success")]
        public async Task<IActionResult> PaymentSuccess(int orderId,string secret)
        {
            if (secret != _config["Payment:Secret"])
                return Unauthorized();
            var order = await work.Orders
                .GetByIdAsync(orderId); // include Items

            if (order == null)
                return NotFound();

            if (order.Status == OrderStatus.Paid)
                return Ok(); // prevent double payment

            // 🔹 Reduce stock
            foreach (var item in order.Items)
            {
                var variant = await work.ProductVariants
                    .GetByIdAsync(item.ProductVariantId);

                if (variant.Quantity < item.Quantity)
                    return BadRequest("Out of stock");

                variant.Quantity -= item.Quantity;
            }

            // 🔹 Mark order paid
            order.Status = OrderStatus.Paid;
            order.PaymentReference = Guid.NewGuid().ToString();

            // 🔹 Clear cart
            var cart = await work.Carts.GetByUserIdAsync(order.UserId);
            cart.Items.Clear();

            await work.SaveChangesAsync();
            return Ok("Payment successful");
        }


        [HttpPost("fail")]
        public async Task<IActionResult> PaymentFail(int orderId)
        {
            var order = await work.Orders.GetByIdAsync(orderId);
            if (order == null)
                return NotFound();

            order.Status = OrderStatus.Failed;

            await work.SaveChangesAsync();
            return Ok("Payment failed");
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> PaymobWebhook([FromBody] dynamic data)
        {
            int orderId = data?.order?.id;
            bool success = data?.success == true;

            var order = await work.Orders.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            if (success)
            {
                order.Status = OrderStatus.Paid;
                order.PaymentReference = data.id;

                var cart = await work.Carts.GetByUserIdAsync(order.UserId);
                cart.Items.Clear();

                // Reduce stock
                foreach (var item in order.Items)
                {
                    var variant = await work.ProductVariants.GetByIdAsync(item.ProductVariantId);
                    variant.Quantity -= item.Quantity;
                }
            }
            else
            {
                order.Status = OrderStatus.Failed;
            }

            await work.SaveChangesAsync();
            return Ok();
        }


    }

}
