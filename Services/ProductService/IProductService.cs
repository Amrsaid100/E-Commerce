using E_Commerce.Dtos.ProductDtos;

namespace E_Commerce.Services.ProductService
{
    public interface IProductService
    {
        Task<ProductDto> AddProductAsync(NewProductDto productDto);
        Task<bool> RemoveProductAsync(int productId);
        Task<bool> UpdateProductAsync(int productId, ProductDto newProduct);
        Task<ProductDto?> GetProductBySearchAsync(string search);
        Task<List<ProductDto>> GetAllProductByCategoryNameAsync(string categoryName);
    }
}
