using ChatbotPlatform.API.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChatbotPlatform.API.Utilities
{
    public static class JwtHelper
    {
        private static IConfiguration? _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string GenerateToken(User user)
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("Invalid jwthelper");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Key"]!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name.DisplayName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("companyId", user.CompanyId!),
                new Claim("userId", user.Id)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationMinutes"]!)),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var range = System.Security.Cryptography.RandomNumberGenerator.Create();
            range.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public static bool ValidateRefreshToken(string refreshToken)
        {
            return !string.IsNullOrEmpty(refreshToken);
        }

        public static string GetUserIdFromRefreshToken(string refreshToken)
        {
            return "placeholder-user-id";
        }
    }
}