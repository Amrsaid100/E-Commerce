using E_Commerce.Dtos.ProductDtos;

namespace E_Commerce.Services.ProductService
{
    public interface IProductService
    {
        Task AddProductAsync(NewProductDto productDto);
        Task RemoveProductAsync(int productId);
        Task UpdateProductAsync(int ProductId, ProductDto NewProduct);
        Task<ProductDto?> GetProductBySearchAsync(string Search);

        Task<List<ProductDto>> GetAllProductByCategoryNameAsync(string CategoryName);
    }
}
