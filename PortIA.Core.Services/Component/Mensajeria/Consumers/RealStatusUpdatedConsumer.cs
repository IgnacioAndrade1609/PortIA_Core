using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Mensajeria.Configuration;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Riesgo.Interfaces;

namespace PortIA.Core.Services.Component.Mensajeria.Consumers;

public class RealStatusUpdatedConsumer(
    IUnitOfWork unitOfWork,
    IRiskEngineService riskEngineService,
    IOptions<DiscrepanciaEtaOptions> discrepanciaEtaOptions,
    ILogger<RealStatusUpdatedConsumer> logger) : IConsumer<RealStatusUpdated>
{
    private const string AccionActualizar = "ACTUALIZAR";
    private const string ClimaMarejada = "Marejada";
    private const string ClimaSinAlerta = "Sin alerta";
    private const string FuenteMock = "MOCK";
    private const string EstadoRetirado = "RETIRADO";
    private const string EstadoDevueltoVacio = "DEVUELTO_VACIO";

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

        container.IdClima = await ResolveClimaIdAsync(evento.WeatherAlert, context.CancellationToken);
        container.UpdateAt = DateTimeOffset.UtcNow;

        if (string.Equals(evento.ScraperSource, FuenteMock, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "RealStatusUpdated con scraper_source={Source} para container_id={ContainerId}: se descarta el estado de puerto y no se recalcula el riesgo.",
                evento.ScraperSource, container.Id);
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
            return;
        }

        container.FechaArriboReal = evento.RealArrivalDate;
        container.EstadoPuerto = evento.PortStatus;

        if (evento.RealArrivalDate is not null)
        {
            container.DeadlineDemurrage = evento.RealArrivalDate.Value.AddDays(container.DiasLibresDemurrage);
        }

        await RegistrarDiscrepanciaEtaSiCorrespondeAsync(container, evento.RealArrivalDate, context.CancellationToken);
        await AplicarEstadoDetectadoAsync(container, evento.EstadoContenedor, context.CancellationToken);

        await riskEngineService.RecalcularAsync(container, context.CancellationToken);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        if (esReinspeccion)
        {
            logger.LogInformation("Reinspección detectada para container_id={ContainerId}, registrando auditoría", container.Id);
            await RegistrarAuditoriaReinspeccionAsync(container, valoresAnteriores!, context.CancellationToken);
        }
    }

    private async Task RegistrarDiscrepanciaEtaSiCorrespondeAsync(Container container, DateTimeOffset? arriboReal, CancellationToken ct)
    {
        if (container.FechaLlegadaEstimada is null || arriboReal is null)
        {
            return;
        }

        var diferenciaHoras = (arriboReal.Value - container.FechaLlegadaEstimada.Value).TotalHours;
        if (Math.Abs(diferenciaHoras) <= discrepanciaEtaOptions.Value.UmbralHoras)
        {
            return;
        }

        var sentido = diferenciaHoras > 0 ? "más tarde" : "más temprano";

        await unitOfWork.Hitos.AddAsync(new Hito
        {
            IdContainer = container.Id,
            Descripcion = $"Discrepancia ETA vs. arribo real: {Math.Abs(diferenciaHoras):F0}h {sentido} que el ETA documental.",
            Origen = "SCRAPER",
            CreadoPor = null,
            FechaOcurrencia = DateTimeOffset.UtcNow,
        }, ct);
    }

    private async Task AplicarEstadoDetectadoAsync(Container container, string? estadoContenedor, CancellationToken ct)
    {
        if (string.Equals(estadoContenedor, EstadoRetirado, StringComparison.OrdinalIgnoreCase)
            && container.FechaRetiroContainer is null)
        {
            var estadoRetirado = await unitOfWork.EstadosContainer.GetByDescripcionAsync(EstadoRetirado, ct);
            if (estadoRetirado is null)
            {
                logger.LogWarning("No se encontró el estado '{Estado}' en el catálogo ESTADO_CONTAINER", EstadoRetirado);
                return;
            }

            var ahora = DateTimeOffset.UtcNow;
            container.FechaRetiroContainer = ahora;
            container.IdEstado = estadoRetirado.Id;
            container.EstadoContainer = estadoRetirado;
            container.DeadlineDetention = ahora.AddDays(container.DiasLibresDetention);

            await unitOfWork.Hitos.AddAsync(new Hito
            {
                IdContainer = container.Id,
                Descripcion = "Retiro del contenedor (gate-out) detectado por el Agente Inspector.",
                Origen = "SCRAPER",
                CreadoPor = null,
                FechaOcurrencia = ahora,
            }, ct);

            return;
        }

        if (string.Equals(estadoContenedor, EstadoDevueltoVacio, StringComparison.OrdinalIgnoreCase)
            && container.FechaRetiroContainer is not null
            && container.EstadoContainer?.Descripcion != EstadoDevueltoVacio)
        {
            var estadoDevueltoVacio = await unitOfWork.EstadosContainer.GetByDescripcionAsync(EstadoDevueltoVacio, ct);
            if (estadoDevueltoVacio is null)
            {
                logger.LogWarning("No se encontró el estado '{Estado}' en el catálogo ESTADO_CONTAINER", EstadoDevueltoVacio);
                return;
            }

            container.IdEstado = estadoDevueltoVacio.Id;
            container.EstadoContainer = estadoDevueltoVacio;

            await unitOfWork.Hitos.AddAsync(new Hito
            {
                IdContainer = container.Id,
                Descripcion = "Devolución del contenedor vacío detectada por el Agente Inspector.",
                Origen = "SCRAPER",
                CreadoPor = null,
                FechaOcurrencia = DateTimeOffset.UtcNow,
            }, ct);
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
