using E_Commerce.Entities;

namespace E_Commerce.Repository
{
    public interface ICartRepo:IGenericRepo<Cart>
    {
        Task<Cart?> GetByUserIdAsync(int userId);
    }
}
