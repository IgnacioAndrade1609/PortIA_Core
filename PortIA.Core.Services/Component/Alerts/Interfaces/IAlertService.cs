namespace PortIA.Core.Services.Component.Alerts.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Alerts.DTOs;

public interface IAlertService
{
    Task<Result<List<AlertResponse>>> GetAlertsAsync(int userId, string userRole, bool? leida, CancellationToken ct);

    Task<Result> MarkAsReadAsync(int alertId, int userId, CancellationToken ct);
}
