using E_Commerce.Dtos.Helpers;
using E_Commerce.Entities;

namespace E_Commerce.Repository
{
    public interface IProductRepo : IGenericRepo<Product>
    {
        Task<List<Product>> GetProductsByCategoryAsync(string categoryname);
        Task<Product> GetProductBySearchAsync(string searchString);
        Task<List<Product>> GetAllWithIncludesAsync();
        Task<(List<Product> products, int totalCount)> GetPagedProductsAsync(PaginationParams paginationParams, string? categoryName = null, string? search = null);
    }
}
