using AutoMapper;
using ChatbotPlatform.API.Data;
using ChatbotPlatform.API.Models.DTOs.Auth;
using ChatbotPlatform.API.Models.Entities;
using ChatbotPlatform.API.Utilities;
using MongoDB.Driver;

namespace ChatbotPlatform.API.Services;

public class AuthService
{
    private readonly MongoDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IMapper _mapper;

    public AuthService(MongoDbContext context, JwtService jwtService, IMapper mapper)
    {
        _context = context;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users.Find(u => u.Email.ToLower() == loginDto.Email.ToLower()).FirstOrDefaultAsync();

        if (user == null || !PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException("Account is not active");
        }

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponseDto
        {
            User = _mapper.Map<UserDto>(user),
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtService.GetTokenExpirationMinutes())
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var user = new User
        {
            Email = registerDto.Email,
            PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
            CompanyId = registerDto.CompanyId,
            Role = registerDto.Role,
            Name = new UserName
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                DisplayName = $"{registerDto.FirstName} {registerDto.LastName}"
            },
            PhoneNumber = registerDto.PhoneNumber,
            Department = registerDto.Department,
            Status = UserStatus.Active,
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.InsertOneAsync(user);

        return new AuthResponseDto
        {
            User = _mapper.Map<UserDto>(user),
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        if (!_jwtService.ValidateRefreshToken(refreshToken))
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var userId = _jwtService.GetUserIdFromRefreshToken(refreshToken);
        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

        if (user == null || user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException("User not found or inactive");
        }

        var newToken = _jwtService.GenerateToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponseDto
        {
            User = _mapper.Map<UserDto>(user),
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtService.GetTokenExpirationMinutes())
        };
    }

    public async Task<UserDto> GetCurrentUserAsync(string userId)
    {
        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

        if (user == null)
        {
            throw new Exception("User not found");
        }

        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> CheckUserExistsAsync(string email)
    {
        var count = await _context.Users.CountDocumentsAsync(u => u.Email.ToLower() == email.ToLower());
        return count > 0;
    }
    public async Task LogoutAsync(string refreshToken)
    {
        if (!_jwtService.ValidateRefreshToken(refreshToken))
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }
    }
}