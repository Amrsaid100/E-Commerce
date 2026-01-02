using E_Commerce.DataContext;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class RevokedTokenRepo : GenericRepo<RevokedToken>, IRevokedTokenRepo
    {
        public RevokedTokenRepo(EcommerceDbContext context) : base(context) { }

        public async Task<bool> ExistsByJtiAsync(string jti)
        {
            return await context.RevokedTokens.AnyAsync(x => x.Jti == jti && x.ExpiresAtUtc > DateTime.UtcNow);
        }
    }
}
