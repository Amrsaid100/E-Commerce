using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.ProductDtos
{
    public class NewProductVariantDto
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
    }
}
