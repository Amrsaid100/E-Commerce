using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitofwork;

        public OrderController(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _unitofwork.Orders.GetAllAsync();
            return Ok(orders);
        }

        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetOrdersByUserId(int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid user ID");

            var orders = await _unitofwork.Orders.GetOrderByUserId(userId);
            if (orders == null || !orders.Items.Any())
                return NotFound("No orders found for this user");

            return Ok(orders);
        }

        [HttpPut("status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus([FromQuery] int orderId, [FromQuery] OrderStatus status)
        {
            if (orderId <= 0)
                return BadRequest("Invalid order ID");

            var order = await _unitofwork.Orders.GetByIdAsync(orderId);
            if (order == null)
                return NotFound("Order not found");

            order.Status = status;
            await _unitofwork.SaveChangesAsync();

            return Ok(order);
        }
    }

}
