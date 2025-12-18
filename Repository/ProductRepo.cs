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
           var searchResult = searchString.Trim().ToLower();
              var product = await context.Products.FirstOrDefaultAsync(p=>p.Description.Trim().ToLower().Contains(searchResult));

            return product;
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string categorname)
        {
            int CategoryId = await context.Categories
                .Where(c => c.Name.ToLower() == categorname.ToLower())
                .Select(c => c.Id)
                .FirstOrDefaultAsync();
            return await context.Products.Where(p => p.CategoryId == CategoryId).ToListAsync();
        }
    }
}
