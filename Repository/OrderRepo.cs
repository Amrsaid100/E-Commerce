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
                .Include(o => o.Governorate)
                .ToListAsync();
        }

        public new async Task<List<Order>> GetAllAsync()
        {
            return await context.Set<Order>()
                .Include(o => o.Items)
                .Include(o => o.User)
                .Include(o => o.Governorate)
                .ToListAsync();
        }

        public async Task<Order?> GetByIdWithItemsAsync(int orderId)
        {
            return await context.Set<Order>()
                .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Governorate)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetOrderByUserId(int userId)
        {
            return await context.Set<Order>()
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                .Where(o => o.UserId == userId)
                .Include(o => o.Governorate)
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
                ItemCount = order.Items != null ? order.Items.Sum(i => i.Quantity) : 0,
                GovernorateId = order.GovernorateId,  // جديد
                GovernorateName = order.Governorate?.NameAr ?? order.Governorate?.NameEn,  // جديد
                ShippingCost = order.ShippingCost,  // جديد
                CreatedAt = order.CreatedAt,
                PhoneNumber = order.PhoneNumber,
                Street = order.Street,
                Neighborhood = order.Neighborhood,
                User = order.User != null ? new Dtos.UserDto.UserDto
                {
                    Id = order.User.Id,
                    Name = order.User.Name,
                    Email = order.User.Email,
                    PhoneNumber = order.User.PhoneNumber
                } : null!
            };
        }
    }
}