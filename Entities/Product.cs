using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Description { get; set; }

        [Required, ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        public decimal Price { get; set; }

        public List<ProductVariant> Variants { get; set; } = new();

        public Category? Category { get; set; }

        public List<ProductImage> Images { get; set; } = new();
    }
}