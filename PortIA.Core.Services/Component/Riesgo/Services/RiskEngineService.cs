namespace PortIA.Core.Services.Component.Riesgo.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Alerts.DTOs;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.Services.Component.Riesgo.Configuration;
using PortIA.Core.Services.Component.Riesgo.Interfaces;

public class RiskEngineService(
    IUnitOfWork unitOfWork,
    IOptions<RiskEngineOptions> options,
    IAlertNotifier alertNotifier,
    ILogger<RiskEngineService> logger) : IRiskEngineService
{
    private const string NivelOk = "OK";
    private const string NivelWarning = "WARNING";
    private const string NivelCritical = "CRITICAL";
    private const string NivelOverdue = "OVERDUE";

    private const string TipoDemurrage = "DEMURRAGE";
    private const string TipoDetention = "DETENTION";

    private const string EstadoDevueltoVacio = "DEVUELTO_VACIO";

    private static readonly Dictionary<string, int> SeverityRank = new()
    {
        [NivelOk] = 0,
        [NivelWarning] = 1,
        [NivelCritical] = 2,
        [NivelOverdue] = 3,
    };

    public async Task<Result> RecalcularAsync(int containerId, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);
        if (container is null)
        {
            return Result.NotFound($"No existe el contenedor {containerId}.");
        }

        await RecalcularAsync(container, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task RecalcularAsync(Container container, CancellationToken ct)
    {
        var (nivel, tipoAlertaDescripcion) = DeterminarNivel(container);

        var rankAnterior = container.Riesgo is not null && SeverityRank.TryGetValue(container.Riesgo.Descripcion, out var rank)
            ? rank
            : -1;

        var nuevoRiesgo = await unitOfWork.Riesgos.GetByDescripcionAsync(nivel, ct)
            ?? throw new InvalidOperationException($"El catálogo RIESGO no tiene una fila '{nivel}'.");

        container.IdRiesgo = nuevoRiesgo.Id;
        container.Riesgo = nuevoRiesgo;
        container.UpdateAt = DateTimeOffset.UtcNow;

        var escalo = tipoAlertaDescripcion is not null && SeverityRank[nivel] > rankAnterior;

        logger.LogInformation(
            "RiskEngine: container_id={ContainerId} nivel={Nivel} escalo={Escalo}",
            container.Id, nivel, escalo);

        if (escalo)
        {
            await CrearAlertaAsync(container, nuevoRiesgo, tipoAlertaDescripcion!, ct);
        }
    }

    private (string Nivel, string? TipoAlerta) DeterminarNivel(Container container)
    {
        if (container.EstadoContainer?.Descripcion == EstadoDevueltoVacio)
        {
            return (NivelOk, null);
        }

        DateTimeOffset? deadlineActivo;
        string tipoAlerta;

        if (container.FechaRetiroContainer is not null)
        {
            deadlineActivo = container.DeadlineDetention;
            tipoAlerta = TipoDetention;
        }
        else if (container.FechaArriboReal is not null)
        {
            deadlineActivo = container.DeadlineDemurrage;
            tipoAlerta = TipoDemurrage;
        }
        else
        {
            return (NivelOk, null);
        }

        if (deadlineActivo is null)
        {
            return (NivelOk, null);
        }

        var horasRestantes = (deadlineActivo.Value - DateTimeOffset.UtcNow).TotalHours;
        var nivel = ClasificarPorHoras(horasRestantes);

        return (nivel, nivel == NivelOk ? null : tipoAlerta);
    }

    private string ClasificarPorHoras(double horasRestantes)
    {
        var umbrales = options.Value;

        if (horasRestantes > umbrales.HorasUmbralOk) return NivelOk;
        if (horasRestantes > umbrales.HorasUmbralWarning) return NivelWarning;
        if (horasRestantes >= umbrales.HorasUmbralCritical) return NivelCritical;
        return NivelOverdue;
    }

    private async Task CrearAlertaAsync(Container container, Riesgo riesgo, string tipoAlertaDescripcion, CancellationToken ct)
    {
        var tipoAlerta = await unitOfWork.TiposAlerta.GetByDescripcionAsync(tipoAlertaDescripcion, ct)
            ?? throw new InvalidOperationException($"El catálogo TIPO_ALERTA no tiene una fila '{tipoAlertaDescripcion}'.");

        var alerta = new Alerta
        {
            IdContainer = container.Id,
            IdTipoAlerta = tipoAlerta.Id,
            IdRiesgo = riesgo.Id,
            Mensaje = $"Contenedor {container.NumeroContainer}: riesgo de {tipoAlertaDescripcion} en nivel {riesgo.Descripcion}.",
        };

        await unitOfWork.Alertas.AddAsync(alerta, ct);
        await unitOfWork.SaveChangesAsync(ct);

        if (container.AsignadoA is not int asignadoA)
        {
            logger.LogInformation(
                "Alerta {AlertId} creada para container_id={ContainerId} sin analista asignado, no se envía notificación en vivo",
                alerta.Id, container.Id);
            return;
        }

        await alertNotifier.NotifyAsync(
            asignadoA,
            new AlertResponse(
                alerta.Id,
                container.Id,
                container.NumeroContainer,
                tipoAlertaDescripcion,
                riesgo.Descripcion,
                alerta.Mensaje,
                alerta.Leida,
                alerta.CreateAt,
                AnalistaAsignado: null),
            ct);
    }
}
