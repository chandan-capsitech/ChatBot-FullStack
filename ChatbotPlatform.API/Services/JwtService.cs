using ChatbotPlatform.API.Models.Entities;
using ChatbotPlatform.API.Utilities;

namespace ChatbotPlatform.API.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        return JwtHelper.GenerateToken(user);
    }

    public string GenerateRefreshToken()
    {
        return JwtHelper.GenerateRefreshToken();
    }

    public bool ValidateRefreshToken(string refreshToken)
    {
        return JwtHelper.ValidateRefreshToken(refreshToken);
    }

    public string GetUserIdFromRefreshToken(string refreshToken)
    {
        return JwtHelper.GetUserIdFromRefreshToken(refreshToken);
    }

    public int GetTokenExpirationMinutes()
    {
        return int.Parse(_configuration["JwtSettings:ExpirationMinutes"]!);
    }
}