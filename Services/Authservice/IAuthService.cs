using E_Commerce.DTOs.Auth;
namespace E_Commerce.Services.Authservice
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}
