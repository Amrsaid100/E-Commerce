using E_Commerce.Entities;

namespace E_Commerce.Repository
{
    public interface IProductRepo:IGenericRepo<Product>
    {
        Task<List<Product>> GetProductsByCategoryAsync(string categoryname);
        Task <Product> GetProductBySearchAsync (string searchString);
    }
}
