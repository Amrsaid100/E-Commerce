using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.CartDto
{
    public class BuyNowDto
    {
        [Required]
        public string Email { get; set; }
        
        [Required]
        public string FullName { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }
        
        public int? GovernorateId { get; set; }
        
        [Required]
        public string Street { get; set; }
        
        [Required]
        public string Building { get; set; }
        
        [Required]
        public string Apartment { get; set; }
        
        [Required]
        public string Neighborhood { get; set; }
        
        // Single item for Buy Now
        [Required]
        public BuyNowItemDto Item { get; set; }
    }

    public class BuyNowItemDto
    {
        public int ProductId { get; set; }
        
        public int? VariantId { get; set; }
        
        [Required]
        public string ProductName { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        public decimal Price { get; set; }
    }
}