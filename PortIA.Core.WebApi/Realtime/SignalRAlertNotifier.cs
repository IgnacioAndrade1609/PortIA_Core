namespace PortIA.Core.WebApi.Realtime;

using Microsoft.AspNetCore.SignalR;
using PortIA.Core.Services.Component.Alerts.DTOs;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.WebApi.Hubs;

public class SignalRAlertNotifier(IHubContext<AlertsHub> hubContext) : IAlertNotifier
{
    public Task NotifyAsync(int userId, AlertResponse alert, CancellationToken ct) =>
        hubContext.Clients.Group(AlertsHub.GroupName(userId)).SendAsync("alertaCreada", alert, ct);
}
