namespace E_Commerce.Dtos.CartDto
{
    public class CartDto
    {
        public List<CartItemDto> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
