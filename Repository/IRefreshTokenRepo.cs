using E_Commerce.Entities;

namespace E_Commerce.Repository
{
    public interface IRefreshTokenRepo : IGenericRepo<RefreshToken>
    {
        Task<RefreshToken?> GetByHashAsync(string hash);
        Task<List<RefreshToken>> GetActiveByUserIdAsync(int userId);
    }
}
