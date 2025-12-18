using E_Commerce.DataContext;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class CartRepo: GenericRepo<Cart>,ICartRepo
    {
        private readonly EcommerceDbContext context;

        public CartRepo(EcommerceDbContext context)
        {
            this.context = context;
        }
        
        public async Task<Cart> GetByUserIdAsync(int userId)
        {
            var Cart= await context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .FirstOrDefaultAsync(c => c.UserId == userId);

            return Cart;
        }
    }
}
