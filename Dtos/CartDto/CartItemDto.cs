using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.CartDto
{
    public class CartItemDto
    {
        public int? ProductVariantId { get; set; } // اجعلها nullable
        public int? ProductId { get; set; } // أضف ProductId
        [Required]
        public string ProductName { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
        
        /// <summary>Available stock for this variant (returned by server only)</summary>
        public int? AvailableStock { get; set; }
    }
}
