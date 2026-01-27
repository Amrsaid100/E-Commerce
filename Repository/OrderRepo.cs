using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using E_Commerce.DataContext;
using E_Commerce.Dtos.UserDto;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class OrderRepo : GenericRepo<Order>, IOrderRepo
    {
        private readonly EcommerceDbContext context;

        public OrderRepo(EcommerceDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Order>> GetAllOrdersWithItemsAndUser()
        {
            return await context.Set<Order>()
                .Include(o => o.Items)
                .Include(o => o.User)
                .ToListAsync();
        }

        public new async Task<List<Order>> GetAllAsync()
        {
            return await context.Set<Order>()
                .Include(o => o.Items)
                .Include(o => o.User)
                .ToListAsync();
        }

        public async Task<Order?> GetByIdWithItemsAsync(int orderId)
        {
            return await context.Set<Order>()
                .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetOrderByUserId(int userId)
        {
            return await context.Set<Order>()
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                .Where(o => o.UserId == userId)
                .ToListAsync();
        }

        public static Dtos.UserDto.UserOrderDto ToUserOrderDto(Entities.Order order)
        {
            return new Dtos.UserDto.UserOrderDto
            {
                OrderId = order.Id,
                Items = order.Items?.Select(i => new Dtos.OrderDto.OrderItemDto
                {
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductName ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitePrice
                }).ToList() ?? new List<Dtos.OrderDto.OrderItemDto>(),
                TotalPrice = order.TotalAmount,
                Status = order.Status.ToString(),
                Email = order.Email,
                City = order.City,
                ItemCount = order.Items != null ? order.Items.Sum(i => i.Quantity) : 0
            };
        }
    }
}