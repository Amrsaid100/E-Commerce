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

        [HttpPut("status")]
        [Authorize(Roles = "Admin,Owner")]
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
