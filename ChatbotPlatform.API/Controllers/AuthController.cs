using ChatbotPlatform.API.Models;
using ChatbotPlatform.API.Models.DTOs.Auth;
using ChatbotPlatform.API.Models.Entities;
using ChatbotPlatform.API.Services;
using ChatbotPlatform.API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatbotPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ApiResponse<AuthResponseDto>> Register(RegisterDto dto)
    {
        var res = new ApiResponse<AuthResponseDto>();
        try
        {
            var existingUser = await _authService.CheckUserExistsAsync(dto.Email);
            if (existingUser)
            {
                res.Message = "User already exists with this email";
                res.Status = false;
                return res;
            }

            var authResponse = await _authService.RegisterAsync(dto);
            res.Status = true;
            res.Message = "User registered successfully";
            res.Result = authResponse;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPost("login")]
    public async Task<ApiResponse<AuthResponseDto>> Login(LoginDto dto)
    {
        var res = new ApiResponse<AuthResponseDto>();
        try
        {
            var user = await _authService.GetByEmailAsync(dto.Email);

            if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash))
            {
                res.Message = "Invalid email or password";
                res.Status = false;
                return res;
            }

            if (user!.Status != UserStatus.Active)
            {
                res.Message = "Account is not active";
                res.Status = false;
                return res;
            }

            var authResponse = await _authService.LoginAsync(dto);
            res.Status = true;
            res.Message = "Login successful";
            res.Result = authResponse;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPost("refresh")]
    public async Task<ApiResponse<AuthResponseDto>> RefreshToken(RefreshTokenDto dto)
    {
        var res = new ApiResponse<AuthResponseDto>();
        try
        {
            var authResponse = await _authService.RefreshTokenAsync(dto.RefreshToken);
            res.Status = true;
            res.Message = "Token refreshed successfully";
            res.Result = authResponse;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ApiResponse<UserDto>> GetCurrentUser()
    {
        var res = new ApiResponse<UserDto>();
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                res.Message = "Invalid token";
                return res;
            }

            var user = await _authService.GetCurrentUserAsync(userId);
            res.Status = true;
            res.Message = "User retrieved successfully";
            res.Result = user;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ApiResponse<object>> Logout(RefreshTokenDto dto)
    {
        var res = new ApiResponse<object>();
        try
        {
            await _authService.LogoutAsync(dto.RefreshToken);
            res.Status = true;
            res.Message = "Logout successful";
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }
}