namespace PortIA.Core.WebApi.Hubs;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class AlertsHub : Hub
{
    public static string GroupName(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId.Value));
        }

        await base.OnConnectedAsync();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirst("sub");
        return userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
}
