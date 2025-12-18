namespace E_Commerce.Dtos.ProductDtos
{
    public class ProductDto
    {
        public string Description { get; set; }
        public decimal Price { get; set; }
        public List<NewProductVariantDto> Variants { get; set; }
        public List<NewProductImageDto> Images { get; set; }
    }
}
