using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Entities
{
    [Index(nameof(CategoryId))]
    [Index(nameof(Name))]
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(300)]
        public string Name { get; set; }

        [Required, MaxLength(200)]
        public string Description { get; set; }

        [Required, ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        public decimal Price { get; set; }

        // Sale fields
        public bool IsOnSale { get; set; } = false;
        public decimal? SalePrice { get; set; }

        // Shipping cost set by Owner/Admin
        public decimal ShippingCost { get; set; } = 0m;

        public List<ProductVariant> Variants { get; set; } = new();

        public Category? Category { get; set; }

        public List<ProductImage> Images { get; set; } = new();
    }
}