using E_Commerce.Dtos.OrderDto;

namespace E_Commerce.Dtos.UserDto
{
    public class UserOrderDto
    {
        public int OrderId { get; set; }
        public List<OrderItemDto> Items { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? Neighborhood { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto User { get; set; }
        public int ItemCount { get; internal set; }
    }
}
