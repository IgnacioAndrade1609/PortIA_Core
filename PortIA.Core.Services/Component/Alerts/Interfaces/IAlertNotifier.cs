namespace PortIA.Core.Services.Component.Alerts.Interfaces;

using PortIA.Core.Services.Component.Alerts.DTOs;

public interface IAlertNotifier
{
    Task NotifyAsync(int userId, AlertResponse alert, CancellationToken ct);
}
