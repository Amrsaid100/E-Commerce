using E_Commerce.Dtos.UserDto;
using E_Commerce.Entities;

namespace E_Commerce.Repository
{
    public interface IOrderRepo: IGenericRepo<Order>
    {
        Task<Order> GetOrderByUserId (int UserId);
    }
}
