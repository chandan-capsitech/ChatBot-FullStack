using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ChatbotPlatform.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task JoinCompanyGroup(string companyId)
    {
        var userCompanyId = Context.User?.FindFirst("companyId")?.Value;

        // Users can only join their own company group
        if (userCompanyId == companyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{companyId}");
        }
    }

    public async Task LeaveCompanyGroup(string companyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Company_{companyId}");
    }

    public async Task SendMessageToCompany(string companyId, string message)
    {
        var userCompanyId = Context.User?.FindFirst("companyId")?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (userCompanyId == companyId)
        {
            await Clients.Group($"Company_{companyId}").SendAsync("ReceiveMessage", userName, message);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var companyId = Context.User?.FindFirst("companyId")?.Value;
        if (!string.IsNullOrEmpty(companyId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{companyId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var companyId = Context.User?.FindFirst("companyId")?.Value;
        if (!string.IsNullOrEmpty(companyId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Company_{companyId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}