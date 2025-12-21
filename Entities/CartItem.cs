using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductVariantId { get; set; }
        [Required]
        public string? ProductName { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
        [Required]
        [ForeignKey("ProductVariant")]
        public virtual ProductVariant? ProductVariant { get; set; }
        [Required]
        [ForeignKey("Cart")]
        public Cart Cart { get; set; }
    }
}
