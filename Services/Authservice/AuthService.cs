using E_Commerce.Dtos.Roles;
using E_Commerce.DTOs.Auth;
using E_Commerce.Entities;
using E_Commerce.Services.JwtServices;
using E_Commerce.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E_Commerce.Services.Authservice
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwt;
        private static readonly Dictionary<string, string> otpStore = new();
        private readonly Random random = new();

        public AuthService(IUnitOfWork uow, IJwtService jwt)
        {
            _uow = uow;
            _jwt = jwt;
            SeedOwnerAsync().Wait();
        }

        // Seed Owner at startup
        private async Task SeedOwnerAsync()
        {
            var ownerEmail = "moaze105@gmail.com"; // >>>>>>>>>>>>>>>>>>>>> Email For Owner
            var existing = await _uow.Users.GetByEmailAsync(ownerEmail);
            if (existing == null)
            {
                var owner = new User
                {
                    Email = ownerEmail,
                    Name = "Owner",
                    Role = UserRole.Owner
                };
                await _uow.Users.AddAsync(owner);
                await _uow.SaveChangesAsync();
            }
        }

        // Generate OTP
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
            }

            var otp = random.Next(100000, 999999).ToString();
            otpStore[dto.Email] = otp;

            // TODO: Replace with real Email sender
            Console.WriteLine($"OTP for {dto.Email}: {otp}");

            return true;
        }

        // Verify OTP & generate JWT
        public async Task<AuthResponseDto?> VerifyOtpAsync(VerifyOtpDto dto)
        {
            if (!otpStore.ContainsKey(dto.Email) || otpStore[dto.Email] != dto.Otp)
                return null;

            otpStore.Remove(dto.Email);

            var user = await _uow.Users.GetByEmailAsync(dto.Email);
            if (user == null) return null;

            var token = _jwt.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString()
            };
        }

        // Promote User to Admin (Owner only)
        public async Task<bool> PromoteUserToAdminAsync(string ownerEmail, string userEmail)
        {
            var owner = await _uow.Users.GetByEmailAsync(ownerEmail);
            if (owner == null || owner.Role != UserRole.Owner)
                return false;

            var user = await _uow.Users.GetByEmailAsync(userEmail);
            if (user == null) return false;

            user.Role = UserRole.Admin;
            await _uow.SaveChangesAsync();
            return true;
        }

        // Demote Admin to User (Owner only)
        public async Task<bool> DemoteAdminToUserAsync(string ownerEmail, string adminEmail)
        {
            var owner = await _uow.Users.GetByEmailAsync(ownerEmail);
            if (owner == null || owner.Role != UserRole.Owner)
                return false;

            var admin = await _uow.Users.GetByEmailAsync(adminEmail);
            if (admin == null || admin.Role != UserRole.Admin)
                return false;

            admin.Role = UserRole.User;
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
