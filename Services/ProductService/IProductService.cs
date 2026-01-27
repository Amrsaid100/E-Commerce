using E_Commerce.Dtos.Helpers;
using E_Commerce.Dtos.ProductDtos;

namespace E_Commerce.Services.ProductService
{
    public interface IProductService
    {
        Task<ProductDto> AddProductAsync(NewProductDto productDto);
        Task<bool> RemoveProductAsync(int productId);
        Task<bool> UpdateProductAsync(int productId, ProductDto newProduct);
        Task<ProductDto?> GetProductBySearchAsync(string search);
        Task<ProductDto?> GetProductByIdAsync(int productId);
        Task<List<ProductDto>> GetAllProductByCategoryNameAsync(string categoryName);
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<PagedResult<ProductDto>> GetPagedProductsAsync(PaginationParams paginationParams, string? categoryName = null, string? search = null);
    }
}
