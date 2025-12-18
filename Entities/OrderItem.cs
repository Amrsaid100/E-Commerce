using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public class OrderItem
    {

        [Key]
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        [Required]
        public string ProductName { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitePrice { get; set; }
        [Required]
        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public Order Order { get; set; }
        [Required]
        [ForeignKey("ProductVariant")]
        public  ProductVariant ProductVariant { get; set; }
    }
}
