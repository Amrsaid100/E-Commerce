using E_Commerce.DataContext;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class CartRepo : GenericRepo<Cart>, ICartRepo
    {
        public CartRepo(EcommerceDbContext context) : base(context)
        {
        }

        public async Task<Cart> GetByUserIdAsync(int userId)
        {
            return await context.Set<Cart>()
                                .Include(c => c.Items)
                                .FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}