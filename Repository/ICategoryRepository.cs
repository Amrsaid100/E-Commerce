using E_Commerce.Entities;
using E_Commerce.Repository;

namespace E_Commerce.Repositories.CategoryRepository
{
    public interface ICategoryRepo : IGenericRepo<Category>
    {
        Task<Category?> GetByNameAsync(string name);

    }
}
