using E_Commerce.Entities;

namespace E_Commerce.Repository
{
    public interface IRevokedTokenRepo : IGenericRepo<RevokedToken>
    {
        Task<bool> ExistsByJtiAsync(string jti);
    }
}
