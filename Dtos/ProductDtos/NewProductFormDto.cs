using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace E_Commerce.Dtos.ProductDtos
{
    public class NewProductFormDto
    {
        public string Name { get; set; }  
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public decimal ShippingCost { get; set; } = 0m;
        
        // Optional: Price and Quantity for single variant
        public decimal VariantPrice { get; set; }
        public int VariantQuantity { get; set; }
        public string VariantColor { get; set; }
        public string VariantSize { get; set; }
        
        // Uploaded image files from form
        public List<IFormFile> Images { get; set; }
    }
}

