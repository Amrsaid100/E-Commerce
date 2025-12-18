using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.OrderDto
{
    public class OrderItemDto
    {
        public int ProductVariantId { get; set; }
        [Required]
        public string ProductName { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
    }
}
