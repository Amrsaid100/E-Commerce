using E_Commerce.Dtos.CartDto;
using E_Commerce.Dtos.UserDto;
using Microsoft.SqlServer.Server;

namespace E_Commerce.Services.UserService
{
    public interface ICartService
    {
        Task AddToCart(int UserId,CartItemDto item);
        Task RemoveFromCart(int UserId,CartItemDto item);
        Task<CartDto> GetUserCart (int UserId); 
        Task ClearCart (int UserId);

        Task <UserOrderDto> CheckOutAsync (int UserId,CheckOutDto CheckOut);
    }
}
