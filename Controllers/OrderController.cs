using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitofwork;

        public OrderController(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
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

            var order = await _unitofwork.Orders.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            if (order.Status == OrderStatus.Cancelled)
                return BadRequest(new { message = "Order is already cancelled" });

            order.Status = OrderStatus.Cancelled;
            await _unitofwork.SaveChangesAsync();

            return Ok(new { message = "Order cancelled successfully", orderId = order.Id, status = order.Status.ToString() });
        }
    }

    // Helper class for status update request
    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}