using E_Commerce.Dtos.OrderDto;

namespace E_Commerce.Dtos.UserDto
{
    public class UserOrderDto
    {
        public List<OrderItemDto> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
