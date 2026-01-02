using E_Commerce.DTOs.Auth;
using E_Commerce.Entities;
using System.Threading.Tasks;

namespace E_Commerce.Services.Authservice
{
    public interface IAuthService
    {
        // Generate & Send OTP
        Task<bool> RequestOtpAsync(RequestOtpDto dto);

        // Verify OTP and return JWT
        Task<AuthResponseDto?> VerifyOtpAsync(VerifyOtpDto dto);

        // Owner-only: Promote User to Admin
        Task<bool> PromoteUserToAdminAsync(string ownerEmail, string userEmail);

        // Owner-only: Demote Admin to User
        Task<bool> DemoteAdminToUserAsync(string ownerEmail, string adminEmail);

        //  Refresh
        Task<RefreshResponseDto?> RefreshAsync(RefreshRequestDto dto);

        // Logout
        Task<bool> LogoutAsync(int userId, string jti, DateTime accessTokenExpiresAtUtc);
    }
}
