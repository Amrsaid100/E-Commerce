using E_Commerce.Entities;

namespace E_Commerce.Repository
{
    public interface IUserRepo : IGenericRepo<User>
    {
        Task<User?> GetByEmailAsync(string email);
    }

}
