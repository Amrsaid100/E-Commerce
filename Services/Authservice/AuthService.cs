using E_Commerce.Dtos.Roles;
using E_Commerce.DTOs.Auth;
using E_Commerce.Entities;
using E_Commerce.Helpers;
using E_Commerce.Services.EmailService;
using E_Commerce.Services.JwtServices;
using E_Commerce.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace E_Commerce.Services.Authservice
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwt;
        private readonly IEmailService _email;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _config;
        private readonly Random _random = new();

        // OTP store with expiry
        private static readonly Dictionary<string, (string Otp, DateTime Expiry)> otpStore = new();

        public AuthService(
            IUnitOfWork uow,
            IJwtService jwt,
            IEmailService email,
            ILogger<AuthService> logger,
            IConfiguration config)
        {
            _uow = uow;
            _jwt = jwt;
            _email = email;
            _logger = logger;
            _config = config;
        }

        // Generate OTP & send email
        public async Task<bool> RequestOtpAsync(RequestOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return false;

            var email = dto.Email.Trim().ToLower();
            var otp = GenerateOtp();

            otpStore[email] = (otp, DateTime.UtcNow.AddMinutes(5));

            _logger.LogInformation($"OTP generated for {email}");

            // Send email in background - DON'T WAIT
            _ = Task.Run(async () =>
            {
                try
                {
                    await _email.SendEmailAsync(email, "Your OTP Code",
                        $"<p>Your OTP is: <b>{otp}</b></p><p>Expires in 5 minutes.</p>");
                    _logger.LogInformation($"Email sent to {email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to send email: {ex.Message}");
                }
            });

            // Return immediately - email sending happens in background
            return true;
        }

        // Verify OTP & generate Access + Refresh
        public async Task<AuthResponseDto?> VerifyOtpAsync(VerifyOtpDto dto)
        {
            if (!otpStore.ContainsKey(dto.Email))
            {
                _logger.LogWarning($"OTP not found for {dto.Email}");
                return null;
            }

            var (storedOtp, expiry) = otpStore[dto.Email];
            if (storedOtp != dto.Otp || expiry < DateTime.UtcNow)
            {
                _logger.LogWarning($"Invalid or expired OTP for {dto.Email}");
                return null;
            }

            otpStore.Remove(dto.Email);

            var user = await _uow.Users.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = dto.Email.Trim(),
                    Name = "",
                    Role = UserRole.User
                };
                await _uow.Users.AddAsync(user);
                await _uow.SaveChangesAsync();
                _logger.LogInformation($"New user created after OTP verification: {user.Email}");
            }


            // 1) Access Token (JWT)
            var accessToken = _jwt.GenerateToken(user);

            // 2) Refresh Token (raw + hash stored)
            var refreshRaw = TokenUtils.GenerateSecureToken();
            var refreshHash = TokenUtils.Sha256(refreshRaw);

            var refreshDays = _config.GetValue<int>("RefreshToken:ExpiryDays");
            if (refreshDays <= 0) refreshDays = 14; // fallback

            var refreshEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(refreshDays)
            };

            await _uow.RefreshTokens.AddAsync(refreshEntity);
            await _uow.SaveChangesAsync();

            _logger.LogInformation($"User {dto.Email} logged in successfully");

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                Token=accessToken,
                RefreshToken = refreshRaw,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Name = user.Name
            };
        }

        // Refresh: rotate refresh token + issue new access
        // NOTE:  DTOs:
        // public record RefreshRequestDto(string RefreshToken);
        // public record RefreshResponseDto(string Token, string RefreshToken);
        public async Task<RefreshResponseDto?> RefreshAsync(RefreshRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return null;

            var hash = TokenUtils.Sha256(dto.RefreshToken);

            // لازم في Repo method: GetByHashAsync
            var stored = await _uow.RefreshTokens.GetByHashAsync(hash);
            if (stored == null)
                return null;

            if (stored.IsExpired || stored.IsRevoked)
                return null;

            var user = await _uow.Users.GetByIdAsync(stored.UserId);
            if (user == null)
                return null;

            // rotate: revoke old + create new
            var newRefreshRaw = TokenUtils.GenerateSecureToken();
            var newRefreshHash = TokenUtils.Sha256(newRefreshRaw);

            stored.RevokedAtUtc = DateTime.UtcNow;
            stored.ReplacedByTokenHash = newRefreshHash;

            var refreshDays = _config.GetValue<int>("RefreshToken:ExpiryDays");
            if (refreshDays <= 0) refreshDays = 14;

            await _uow.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(refreshDays)
            });

            // new access token
            var newAccess = _jwt.GenerateToken(user);

            await _uow.SaveChangesAsync();

            return new RefreshResponseDto(newAccess, newRefreshRaw);
        }

        // Logout: revoke access (by jti) + revoke all active refresh tokens for user
        
        public async Task<bool> LogoutAsync(int userId, string jti, DateTime accessTokenExpiresAtUtc)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(jti))
                return false;

            // revoke access jti (blacklist)
            var already = await _uow.RevokedTokens.ExistsByJtiAsync(jti);
            if (!already)
            {
                await _uow.RevokedTokens.AddAsync(new RevokedToken
                {
                    Jti = jti,
                    ExpiresAtUtc = accessTokenExpiresAtUtc,
                    Reason = "logout"
                });
            }

            // revoke all active refresh tokens for this user
            var activeTokens = await _uow.RefreshTokens.GetActiveByUserIdAsync(userId);
            foreach (var t in activeTokens)
            {
                t.RevokedAtUtc = DateTime.UtcNow;
            }

            await _uow.SaveChangesAsync();
            return true;
        }

        // Promote User to Admin (Owner only)
        public async Task<bool> PromoteUserToAdminAsync(string ownerEmail, string userEmail)
        {
            var owner = await _uow.Users.GetByEmailAsync(ownerEmail);
            if (owner == null || owner.Role != UserRole.Owner)
            {
                _logger.LogWarning($"{ownerEmail} attempted to promote without Owner role");
                return false;
            }

            var user = await _uow.Users.GetByEmailAsync(userEmail);
            if (user == null) return false;

            user.Role = UserRole.Admin;
            await _uow.SaveChangesAsync();
            _logger.LogInformation($"User {userEmail} promoted to Admin by {ownerEmail}");
            return true;
        }

        // Demote Admin to User (Owner only)
        public async Task<bool> DemoteAdminToUserAsync(string ownerEmail, string adminEmail)
        {
            var owner = await _uow.Users.GetByEmailAsync(ownerEmail);
            if (owner == null || owner.Role != UserRole.Owner)
            {
                _logger.LogWarning($"{ownerEmail} attempted to demote without Owner role");
                return false;
            }

            var admin = await _uow.Users.GetByEmailAsync(adminEmail);
            if (admin == null || admin.Role != UserRole.Admin)
                return false;

            admin.Role = UserRole.User;
            await _uow.SaveChangesAsync();
            _logger.LogInformation($"Admin {adminEmail} demoted to User by {ownerEmail}");
            return true;
        }
        // Add this private method inside the AuthService class

        private string GenerateOtp()
        {
            // Generates a 6-digit numeric OTP
            return _random.Next(100000, 999999).ToString();
        }
    }
}
