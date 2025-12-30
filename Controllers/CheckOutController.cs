using E_Commerce.Dtos.UserDto;
using E_Commerce.Services.PaymentService;
using E_Commerce.Services.UserService;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckOutController : ControllerBase
    {
        public class CheckoutController : ControllerBase
        {
            private readonly ICartService _cartService;
            private readonly IPaymentService _paymentService;
            private readonly IUnitOfWork _Work;
            public CheckoutController(
                ICartService _cartService,
                IPaymentService paymentService,
                IUnitOfWork _work)
            {
                this._cartService = _cartService;
                this._paymentService = paymentService;
                this._Work = _work;
            }

            [HttpPost]
            public async Task<IActionResult> Checkout(CheckOutDto dto)
            {
                int userId = int.Parse(User.FindFirst("id").Value);

                int orderId = await _cartService.CheckOutAsync(userId, dto);
                if (orderId == 0) return BadRequest("Cart is empty");

                var order = await _Work.Orders.GetByIdAsync(orderId); // get order entity

                string paymentUrl = await _paymentService.CreatePaymentUrl(orderId);

                return Ok(new
                {
                    orderId,
                    paymentUrl
                });
            }


        }
    }
}
