using E_Commerce.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Dtos.ProductDtos
{
    public class NewProductDto
    {
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public List<NewProductVariantDto> Variants { get; set; }
        public Category Category { get; set; }
        public List<NewProductImageDto> Images { get; set; }
    }
}
