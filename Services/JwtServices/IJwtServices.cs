using E_Commerce.Entities;
namespace E_Commerce.Services.JwtServices
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
