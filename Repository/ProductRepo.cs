using E_Commerce.DataContext;
using E_Commerce.Dtos.Helpers;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class ProductRepo : GenericRepo<Product>, IProductRepo
    {
        private readonly EcommerceDbContext context;
        public ProductRepo(EcommerceDbContext context) : base(context)
        {
            this.context = context;
        }

        // Override GetByIdAsync to include related data (Images, Variants, Category)
        public override async Task<Product> GetByIdAsync(int id)
        {
            return await context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> GetProductBySearchAsync(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return null!;

            var searchResult = searchString.Trim().ToLower();

            // Guard against null Description values to avoid NullReferenceException
            var product = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => (p.Name != null && p.Name.Trim().ToLower().Contains(searchResult)) ||
                           (p.Description != null && p.Description.Trim().ToLower().Contains(searchResult)))
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

        public async Task<(List<Product> products, int totalCount)> GetPagedProductsAsync(
            PaginationParams paginationParams,
            string? categoryName = null,
            string? search = null)
        {
            var query = context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsQueryable();

            // Apply category filter if provided
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var normalized = categoryName.Trim().ToLower();
                query = query.Where(p => p.Category != null && p.Category.Name.ToLower() == normalized);
            }

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(searchLower)) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var products = await query
                .OrderBy(p => p.Id)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return (products, totalCount);
        }
    }
}
