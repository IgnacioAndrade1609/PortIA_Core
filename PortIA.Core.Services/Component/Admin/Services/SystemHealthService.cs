namespace PortIA.Core.Services.Component.Admin.Services;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Admin.DTOs;
using PortIA.Core.Services.Component.Admin.Interfaces;

public class SystemHealthService(IUnitOfWork unitOfWork) : ISystemHealthService
{
    private const string ExtraccionRevisar = "NEEDS_REVIEW";
    private const string ExtraccionPendiente = "PENDIENTE";
    private const string CargaError = "ERROR";
    private const string CargaProcesando = "PROCESANDO";
    private const string NotificacionFallida = "FALLIDO";
    private const string NotificacionFallidaAutomatica = "FALLIDO_AUTOMATICO";

    public async Task<Result<SystemHealthResponse>> GetAsync(CancellationToken ct)
    {
        var ahora = DateTimeOffset.UtcNow;
        var haceUnaHora = ahora.AddHours(-1);
        var hace24Horas = ahora.AddHours(-24);
        var hace7Dias = ahora.AddDays(-7);

        var sinAsignar = await unitOfWork.Containers.Query()
            .CountAsync(c => c.AsignadoA == null, ct);

        var conAnalistaInactivo = await unitOfWork.Containers.Query()
            .CountAsync(c => c.Asignado != null && !c.Asignado.Vigente, ct);

        var porRevisar = await unitOfWork.Documentos.Query()
            .CountAsync(d => d.Vigente && d.EstadoExtraccion == ExtraccionRevisar, ct);

        var pendientesAtascados = await unitOfWork.Documentos.Query()
            .CountAsync(d => d.Vigente && d.EstadoExtraccion == ExtraccionPendiente && d.CreateAt < haceUnaHora, ct);

        var cargasConError = await unitOfWork.CargasDocumentos.Query()
            .CountAsync(c => c.Vigente && c.Status == CargaError, ct);

        var cargasAtascadas = await unitOfWork.CargasDocumentos.Query()
            .CountAsync(c => c.Vigente && c.Status == CargaProcesando && c.UpdateAt < haceUnaHora, ct);

        var notificacionesFallidas = await unitOfWork.LogsNotificacion.Query()
            .CountAsync(l => (l.Estado == NotificacionFallida || l.Estado == NotificacionFallidaAutomatica) && l.CreateAt >= hace7Dias, ct);

        var alertasSinLeer = await unitOfWork.Alertas.Query()
            .CountAsync(a => !a.Leida && a.CreateAt < hace24Horas, ct);

        return Result<SystemHealthResponse>.Success(new SystemHealthResponse(
            sinAsignar,
            conAnalistaInactivo,
            porRevisar,
            pendientesAtascados,
            cargasConError,
            cargasAtascadas,
            notificacionesFallidas,
            alertasSinLeer,
            ahora));
    }
}
