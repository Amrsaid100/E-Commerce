using E_Commerce.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace E_Commerce.Services.JwtServices
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _key;
        private readonly int _expiryMinutes;

        public JwtService(IConfiguration config)
        {
            _config = config;
            _issuer = _config["Jwt:Issuer"] ?? "ECommerceIssuer";
            _audience = _config["Jwt:Audience"] ?? "ECommerceAudience";
            _key = _config["Jwt:Key"] ?? "ReplaceWithStrongKey";
            _expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60;
        }

        public string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                // Fix for CS1061: 'User' does not contain a definition for 'Role'
                // Use a default role value since User does not have a Role property
                new Claim(ClaimTypes.Role, "User")
            };

            // Fix for IDE0090: 'new' expression can be simplified
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
