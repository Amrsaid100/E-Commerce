using E_Commerce.DTOs.Auth;

namespace E_Commerce.Services.Authservice
{
    public interface IAuthService
    {
        Task<bool> RequestOtpAsync(RequestOtpDto dto);      // Generate & Send OTP
        Task<AuthResponseDto?> VerifyOtpAsync(VerifyOtpDto dto); // Verify OTP and return JWT
    }
}
