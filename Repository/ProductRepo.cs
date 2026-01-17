using E_Commerce.DataContext;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class ProductRepo:GenericRepo<Product>,IProductRepo
    {
        private readonly EcommerceDbContext context;
        public ProductRepo(EcommerceDbContext context):base(context)
        {
            this.context = context;
        }

        public async Task<Product> GetProductBySearchAsync(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return null!;

            var searchResult = searchString.Trim().ToLower();

            // Guard against null Description values to avoid NullReferenceException
            var product = await context.Products
                .Where(p => p.Description != null && p.Description.Trim().ToLower().Contains(searchResult))
                .FirstOrDefaultAsync();

            return product;
        }
        public async Task<List<Product>> GetProductsByCategoryAsync(string categorname)
        {
            if (string.IsNullOrWhiteSpace(categorname))
                return new List<Product>();

            var normalized = categorname.Trim().ToLower();

            // Find matching category id safely (guard against null names)
            int CategoryId = await context.Categories
                .Where(c => c.Name != null && c.Name.ToLower() == normalized)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (CategoryId == 0)
                return new List<Product>();

            return await context.Products
                .Where(p => p.CategoryId == CategoryId)
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .ToListAsync();
        }

        public async Task<List<Product>> GetAllWithIncludesAsync()
        {
            return await context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .OrderBy(p => p.Id)
                .ToListAsync();
        }
    }
}
