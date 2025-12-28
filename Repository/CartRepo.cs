using E_Commerce.DataContext;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E_Commerce.Repository
{
    public class CartRepo : GenericRepo<Cart>, ICartRepo
    {
        public CartRepo(EcommerceDbContext context) : base(context)
        {
        }

        // Get cart by userId with items & product variants
        public async Task<Cart?> GetByUserIdAsync(int userId)
        {
            return await context.Set<Cart>()
                                .Include(c => c.Items)
                                    .ThenInclude(i => i.ProductVariant)
                                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        // Get all products in the cart
        public async Task<List<ProductVariant>> GetProductsInCartAsync(int userId)
        {
            var cart = await GetByUserIdAsync(userId);
            if (cart == null || cart.Items == null) return new List<ProductVariant>();

            return cart.Items
                       .Where(i => i.ProductVariant != null)
                       .Select(i => i.ProductVariant!)
                       .ToList();
        }

        // Get total price of the cart
        public async Task<decimal> GetCartTotalAsync(int userId)
        {
            var cart = await GetByUserIdAsync(userId);
            if (cart == null || cart.Items == null) return 0m;

            return cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        }
    }
}
