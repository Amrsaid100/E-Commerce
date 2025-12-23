using E_Commerce.DataContext;
using E_Commerce.Entities;
using E_Commerce.Repository;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repositories.CategoryRepository
{
    public class CategoryRepo : GenericRepo<Category>, ICategoryRepo
    {
        private readonly EcommerceDbContext context;

        public CategoryRepo(EcommerceDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await context.Categories
                                .FirstOrDefaultAsync(c =>
                                    c.Name != null &&
                                    c.Name.ToLower() == name.ToLower());
        }
    }
}
