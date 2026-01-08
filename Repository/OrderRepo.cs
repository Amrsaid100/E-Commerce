using E_Commerce.DataContext;
using E_Commerce.Dtos.UserDto;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class OrderRepo:GenericRepo<Order>,IOrderRepo
    {
        private readonly EcommerceDbContext context;
        public OrderRepo(EcommerceDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Order>> GetOrderByUserId(int UserId)
        {
             return await context.Set<Order>().Include(o=>o.Items)
                                           .ThenInclude(i=>i.ProductVariant)
                                           .Where(o=>o.UserId==UserId)
                                           .ToListAsync();
        }
    }
}
