using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatbotPlatform.API.Models;
using ChatbotPlatform.API.Models.DTOs.Chat;
using ChatbotPlatform.API.Services;
using System.Security.Claims;

namespace ChatbotPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("sessions/active")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<List<ChatSessionDto>>> GetActiveSessions()
    {
        var res = new ApiResponse<List<ChatSessionDto>>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value!;

            var sessions = await _chatService.GetActiveSessionsAsync(currentUserCompanyId);
            res.Status = true;
            res.Message = "Active chat sessions retrieved successfully";
            res.Result = sessions.ToList();
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("sessions/employee")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<List<ChatSessionDto>>> GetSessionsByEmployee()
    {
        var res = new ApiResponse<List<ChatSessionDto>>();
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

            var sessions = await _chatService.GetSessionsByEmployeeAsync(currentUserId);
            res.Status = true;
            res.Message = "Employee chat sessions retrieved successfully";
            res.Result = sessions.ToList();
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPost("sessions/{sessionId}/assign")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<object>> AssignSession(string sessionId)
    {
        var res = new ApiResponse<object>();
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

            await _chatService.AssignAgentAsync(sessionId, currentUserId);
            res.Status = true;
            res.Message = "Chat session assigned successfully";
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPost("sessions/{sessionId}/close")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<object>> CloseSession(string sessionId)
    {
        var res = new ApiResponse<object>();
        try
        {
            await _chatService.CloseSessionAsync(sessionId);
            res.Status = true;
            res.Message = "Chat session closed successfully";
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("sessions/{sessionId}")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<ChatSessionDto>> GetSession(string sessionId)
    {
        var res = new ApiResponse<ChatSessionDto>();
        try
        {
            var session = await _chatService.GetSessionAsync(sessionId);

            // Check if user has access to this session's company
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != session.CompanyId)
            {
                res.Status = false;
                res.Message = "Access denied to this chat session";
                return res;
            }

            res.Status = true;
            res.Message = "Chat session retrieved successfully";
            res.Result = session;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }
}