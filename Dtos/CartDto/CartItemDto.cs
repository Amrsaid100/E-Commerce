using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.CartDto
{
    public class CartItemDto
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
