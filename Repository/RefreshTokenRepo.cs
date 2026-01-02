using E_Commerce.DataContext;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class RefreshTokenRepo : GenericRepo<RefreshToken>, IRefreshTokenRepo
    {
        public RefreshTokenRepo(EcommerceDbContext context) : base(context) { }

        public async Task<RefreshToken?> GetByHashAsync(string hash)
        {
            return await context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        }

        public async Task<List<RefreshToken>> GetActiveByUserIdAsync(int userId)
        {
            return await context.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync();
        }
    }
}
