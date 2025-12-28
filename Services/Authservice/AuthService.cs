using E_Commerce.Dtos.Roles;
using E_Commerce.DTOs.Auth;
using E_Commerce.Entities;
using E_Commerce.Services.EmailService;
using E_Commerce.Services.JwtServices;
using E_Commerce.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E_Commerce.Services.Authservice
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwt;
        private readonly IEmailService _email;
        private readonly ILogger<AuthService> _logger;
        private readonly Random _random = new();

        // OTP store with expiry
        private static readonly Dictionary<string, (string Otp, DateTime Expiry)> otpStore = new();

        public AuthService(IUnitOfWork uow, IJwtService jwt, IEmailService email, ILogger<AuthService> logger)
        {
            _uow = uow;
            _jwt = jwt;
            _email = email;
            _logger = logger;
        }

        // Generate OTP & send email
        public async Task<bool> RequestOtpAsync(RequestOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return false;

            var user = await _uow.Users.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = dto.Email,
                    Name = "",
                    Role = UserRole.User
                };
                await _uow.Users.AddAsync(user);
                await _uow.SaveChangesAsync();
                _logger.LogInformation($"New user created with Email: {dto.Email}");
            }

            var otp = _random.Next(100000, 999999).ToString();
            otpStore[dto.Email] = (otp, DateTime.UtcNow.AddMinutes(5));
            _logger.LogInformation($"OTP generated for {dto.Email}");

            // Send OTP via email
            await _email.SendEmailAsync(dto.Email, "Your OTP Code", $"Your OTP is: {otp}");

            return true;
        }

        // Verify OTP & generate JWT
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
            if (user == null) return null;

            var token = _jwt.GenerateToken(user);
            _logger.LogInformation($"User {dto.Email} logged in successfully");

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Name = user.Name
            };
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
    }
}
