using ChatbotPlatform.API.Exceptions;
using ChatbotPlatform.API.Models;
using ChatbotPlatform.API.Models.DTOs.Auth;
using ChatbotPlatform.API.Models.Entities;
using ChatbotPlatform.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatbotPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly CompanyService _companyService;

    public UserController(UserService userService, CompanyService companyService)
    {
        _userService = userService;
        _companyService = companyService;
    }

    [HttpGet]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<List<UserDto>>> GetAll()
    {
        var res = new ApiResponse<List<UserDto>>();
        try
        {
            var users = await _userService.GetAllAsync();
            res.Status = true;
            res.Message = "Users retrieved successfully";
            res.Result = users;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("company/{companyId}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<List<UserDto>>> GetByCompany(string companyId)
    {
        var res = new ApiResponse<List<UserDto>>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Company Admins can see ALL users in their company (including other admins)
            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != companyId)
            {
                res.Status = false;
                res.Message = "Access denied to this company's users";
                return res;
            }

            var users = await _userService.GetByCompanyIdAsync(companyId);

            var message = currentUserRole == "Admin" ? $"Retrieved {users.Count()} users from your company (you can manage all of them)" : "Users retrieved successfully";

            res.Status = true;
            res.Message = message;
            res.Result = users;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<UserDto>> GetById(string id)
    {
        var res = new ApiResponse<UserDto>();
        try
        {
            var user = await _userService.GetByIdAsync(id);

            // Check access permissions
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != user.CompanyId)
            {
                res.Status = false;
                res.Message = "Access denied to this user";
                return res;
            }

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

    [HttpPost]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<UserDto>> Create(CreateUserDto dto)
    {
        var res = new ApiResponse<UserDto>();
        try
        {
            // Check if current user can create users for this company
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // if company status is not active
            var currentStatus = await _companyService.GetCompanyStatusAsync(currentUserCompanyId!);
            if(currentStatus != "Active")
            {
                res.Status = false;
                res.Message = "Company Id is inactive or suspended";
                return res;
            }

            // SuperAdmin can create users for ANY company (except SuperAdmin role)
            if (currentUserRole == "SuperAdmin")
            {
                if (dto.Role == UserRole.SuperAdmin)
                {
                    res.Status = false;
                    res.Message = "Cannot create SuperAdmin users through API";
                    return res;
                }
            }
            // Company Admin can create BOTH Admins AND Employees for THEIR OWN company
            else if (currentUserRole == "Admin")
            {
                // Auto-set company ID to current user's company
                dto.CompanyId = currentUserCompanyId!;

                // Company Admin cannot create SuperAdmin users
                if (dto.Role == UserRole.SuperAdmin)
                {
                    res.Status = false;
                    res.Message = "Company Admins cannot create SuperAdmin users";
                    return res;
                }

                // Company Admin CAN create both Admin and Employee roles
                if (dto.Role != UserRole.Admin && dto.Role != UserRole.Employee)
                {
                    res.Status = false;
                    res.Message = "Company Admins can only create Admin or Employee users";
                    return res;
                }
            }
            // Employees cannot create users
            else
            {
                res.Status = false;
                res.Message = "Employees are not authorized to create users";
                return res;
            }

            // Set CreatedBy to current user
            dto.CreatedBy = currentUserId;

            var user = await _userService.CreateAsync(dto);

            res.Status = true;
            res.Message = $"{dto.Role} created successfully for your company";
            res.Result = user;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<UserDto>> Update(string id, UpdateUserDto dto)
    {
        var res = new ApiResponse<UserDto>();
        try
        {
            // Get the target user first
            var existingUser = await _userService.GetByIdAsync(id);
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Enhanced permission check
            if (currentUserRole != "SuperAdmin")
            {
                // Company Admin can update users in their own company
                if (currentUserCompanyId != existingUser.CompanyId)
                {
                    res.Status = false;
                    res.Message = "Access denied to update this user";
                    return res;
                }

                // Company Admin cannot demote themselves
                if (currentUserId == id && dto.Status == UserStatus.Inactive)
                {
                    res.Status = false;
                    res.Message = "Cannot deactivate your own account";
                    return res;
                }
            }

            var user = await _userService.UpdateAsync(id, dto);
            res.Status = true;
            res.Message = "User updated successfully";
            res.Result = user;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<object>> Delete(string id)
    {
        var res = new ApiResponse<object>();
        try
        {
            // Get the user first to check permissions
            var existingUser = await _userService.GetByIdAsync(id);
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != existingUser.CompanyId)
            {
                res.Status = false;
                res.Message = "Access denied to delete this user";
                return res;
            }

            // Prevent users from deleting themselves
            if (currentUserId == id)
            {
                res.Status = false;
                res.Message = "Cannot delete your own account";
                return res;
            }

            await _userService.DeleteAsync(id);
            res.Status = true;
            res.Message = "User deleted successfully";
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<UserDto>> UpdateStatus(string id, UpdateUserStatusDto dto)
    {
        var res = new ApiResponse<UserDto>();
        try
        {
            // Get the user first to check permissions
            var existingUser = await _userService.GetByIdAsync(id);
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != existingUser.CompanyId)
            {
                res.Status = false;
                res.Message = "Access denied to update this user's status";
                return res;
            }

            // Prevent users from deactivating themselves
            if (currentUserId == id && dto.Status == UserStatus.Inactive)
            {
                res.Status = false;
                res.Message = "Cannot deactivate your own account";
                return res;
            }

            var user = await _userService.UpdateStatusAsync(id, dto.Status);
            res.Status = true;
            res.Message = "User status updated successfully";
            res.Result = user;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("role/{role}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<List<UserDto>>> GetByRole(UserRole role)
    {
        var res = new ApiResponse<List<UserDto>>();
        try
        {
            var users = await _userService.GetByRoleAsync(role);
            res.Status = true;
            res.Message = $"Users with role {role} retrieved successfully";
            res.Result = users;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }
}