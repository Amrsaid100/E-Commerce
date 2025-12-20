using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.OrderDto
{
    public class OrderItemDto
    {
        public int ProductVariantId { get; set; }
        
        public string ProductName { get; set; }
       
        public int Quantity { get; set; }
      
        public decimal UnitPrice { get; set; }
    }
}
