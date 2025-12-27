using E_Commerce.Entities;
using E_Commerce.Services.JwtServices;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace E_Commerce.Services.JwtServices
{
    public class JwtService : IJwtService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _key;
        private readonly int _expiryMinutes;

        public JwtService(IConfiguration config)
        {
            _issuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
            _audience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");
            _key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            _expiryMinutes = int.TryParse(config["Jwt:ExpiryMinutes"], out var m) ? m : 30;
        }

        public string GenerateToken(User user)
        {
            var claims = new List<Claim>
        {
            // user id
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            // email
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            // role
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            // jti revoke
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}
