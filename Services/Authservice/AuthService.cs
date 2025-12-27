using E_Commerce.DTOs.Auth;
using E_Commerce.Entities;
using E_Commerce.Services.JwtServices;
using E_Commerce.UnitOfWork;

namespace E_Commerce.Services.Authservice
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwt;

        // In-memory OTP store (for simplicity). 
        // For production, use DB or Redis with expiry.
        private static readonly Dictionary<string, string> otpStore = new();

        private readonly Random random = new();

        public AuthService(IUnitOfWork uow, IJwtService jwt)
        {
            _uow = uow;
            _jwt = jwt;
        }

        // Step 1: Generate and send OTP
        public async Task<bool> RequestOtpAsync(RequestOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return false;

            // Check if user exists, if not create a new User
            var user = await _uow.Users.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = dto.Email,
                    Name = "",
                    Role = "User"
                };
                await _uow.Users.AddAsync(user);
                await _uow.SaveChangesAsync();
            }

            // Generate 6-digit OTP
            var otp = random.Next(100000, 999999).ToString();

            // Store OTP (for simplicity in memory)
            otpStore[dto.Email] = otp;

            // TODO: Send OTP via Email service
            Console.WriteLine($"OTP for {dto.Email}: {otp}"); // replace with real email sender

            return true;
        }

        // Step 2: Verify OTP and generate JWT
        public async Task<AuthResponseDto?> VerifyOtpAsync(VerifyOtpDto dto)
        {
            if (!otpStore.ContainsKey(dto.Email))
                return null;

            if (otpStore[dto.Email] != dto.Otp)
                return null;

            // OTP valid, remove it from store
            otpStore.Remove(dto.Email);

            // Get the user
            var user = await _uow.Users.GetByEmailAsync(dto.Email);
            if (user == null)
                return null;

            // Generate JWT
            var token = _jwt.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role
            };
        }
    }
}
