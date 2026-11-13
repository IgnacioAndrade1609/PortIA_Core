namespace PortIA.Core.Services.Component.Alerts.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Alerts.DTOs;
using PortIA.Core.Services.Component.Alerts.Interfaces;

public class AlertService(
    IUnitOfWork unitOfWork,
    ILogger<AlertService> logger) : IAlertService
{
    public async Task<Result<List<AlertResponse>>> GetAlertsAsync(int userId, string userRole, bool? leida, CancellationToken ct)
    {
        var query = unitOfWork.Alertas.Query();

        var normalizedRole = userRole.ToUpperInvariant();
        if (normalizedRole is "ANALIST" or "ANALISTA")
        {
            query = query.Where(a => a.Container.AsignadoA == userId);
        }
        else if (normalizedRole is "BOSS" or "JEFE")
        {
            query = query.Where(a => a.Container.Asignado != null && a.Container.Asignado.JefeId == userId);
        }

        if (leida.HasValue)
        {
            query = query.Where(a => a.Leida == leida.Value);
        }

        var alertas = await query
            .OrderByDescending(a => a.CreateAt)
            .Select(a => new AlertResponse(
                a.Id,
                a.IdContainer,
                a.Container.NumeroContainer,
                a.TipoAlerta.Descripcion,
                a.Riesgo.Descripcion,
                a.Mensaje,
                a.Leida,
                a.CreateAt,
                a.Container.Asignado != null ? a.Container.Asignado.NombreUsuario : null))
            .ToListAsync(ct);

        return Result<List<AlertResponse>>.Success(alertas);
    }

    public async Task<Result> MarkAsReadAsync(int alertId, int userId, CancellationToken ct)
    {
        var alerta = await unitOfWork.Alertas.GetByIdAsync(alertId, ct);
        if (alerta is null || alerta.Container.AsignadoA != userId)
        {
            logger.LogWarning(
                "MarkAsRead rechazado: alerta {AlertId} no existe o no pertenece a userId={UserId}",
                alertId, userId);
            return Result.NotFound($"La alerta {alertId} no existe o no pertenece al usuario.");
        }

        if (!alerta.Leida)
        {
            alerta.Leida = true;
            alerta.UsuarioLector = userId;
            alerta.FechaLectura = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}
