namespace PortIA.Core.WebApi.Realtime;

using PortIA.Core.Services.Component.Alerts.DTOs;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.Services.Component.Notifications.Services;

public class CompositeAlertNotifier(
    SignalRAlertNotifier signalR,
    AlertEmailNotifier email) : IAlertNotifier
{
    public async Task NotifyAsync(int userId, AlertResponse alert, CancellationToken ct)
    {
        await signalR.NotifyAsync(userId, alert, ct);
        await email.NotifyAsync(userId, alert, ct);
    }
}
