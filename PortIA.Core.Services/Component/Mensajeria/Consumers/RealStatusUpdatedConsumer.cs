using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Mensajeria.Events;

namespace PortIA.Core.Services.Component.Mensajeria.Consumers;

public class RealStatusUpdatedConsumer(
    IUnitOfWork unitOfWork,
    ILogger<RealStatusUpdatedConsumer> logger) : IConsumer<RealStatusUpdated>
{
    private const string AccionActualizar = "ACTUALIZAR";
    private const string ClimaMarejada = "Marejada";
    private const string ClimaSinAlerta = "Sin alerta";

    public async Task Consume(ConsumeContext<RealStatusUpdated> context)
    {
        var evento = context.Message;
        logger.LogInformation("RealStatusUpdated recibido: container_id={ContainerId}, source={Source}",
            evento.ContainerId, evento.ScraperSource);

        var container = await unitOfWork.Containers.GetByIdAsync(evento.ContainerId, context.CancellationToken);

        if (container is null)
        {
            logger.LogWarning("Container {ContainerId} no existe, se descarta el evento", evento.ContainerId);
            return;
        }

        var esReinspeccion = container.FechaArriboReal is not null;
        var valoresAnteriores = esReinspeccion ? BuildAuditSnapshot(container) : null;

        container.FechaArriboReal = evento.RealArrivalDate;
        container.EstadoPuerto = evento.PortStatus;
        container.IdClima = await ResolveClimaIdAsync(evento.WeatherAlert, context.CancellationToken);
        container.UpdateAt = DateTimeOffset.UtcNow;

        if (evento.RealArrivalDate is not null)
        {
            container.DeadlineDemurrage = evento.RealArrivalDate.Value.AddDays(container.DiasLibresDemurrage);
        }

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        if (esReinspeccion)
        {
            logger.LogInformation("Reinspección detectada para container_id={ContainerId}, registrando auditoría", container.Id);
            await RegistrarAuditoriaReinspeccionAsync(container, valoresAnteriores!, context.CancellationToken);
        }
    }

    private async Task<int?> ResolveClimaIdAsync(bool weatherAlert, CancellationToken ct)
    {
        var descripcion = weatherAlert ? ClimaMarejada : ClimaSinAlerta;
        var clima = await unitOfWork.Climas.GetByDescripcionAsync(descripcion, ct);
        return clima?.Id;
    }

    private async Task RegistrarAuditoriaReinspeccionAsync(Container container, string valoresAnteriores, CancellationToken ct)
    {
        var accion = await unitOfWork.Acciones.GetByDescripcionAsync(AccionActualizar, ct);
        if (accion is null)
        {
            logger.LogWarning("No se encontró la acción '{Accion}', no se registra auditoría para container_id={ContainerId}",
                AccionActualizar, container.Id);
            return;
        }

        await unitOfWork.LogsAuditoria.AddAsync(new LogAuditoria
        {
            NombreEntidad = nameof(Container),
            IdEntidad = container.Id,
            IdAccion = accion.Id,
            Responsable = null,
            ValoresAnteriores = valoresAnteriores,
            ValoresNuevos = BuildAuditSnapshot(container),
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }

    private static string BuildAuditSnapshot(Container container) => JsonSerializer.Serialize(new
    {
        container.FechaArriboReal,
        container.EstadoPuerto,
        container.IdClima,
    });
}
