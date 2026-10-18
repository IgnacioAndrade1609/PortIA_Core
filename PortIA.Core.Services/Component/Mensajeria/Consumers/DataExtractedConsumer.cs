using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;

namespace PortIA.Core.Services.Component.Mensajeria.Consumers;

public class DataExtractedConsumer(
    IUnitOfWork unitOfWork,
    IInspectContainerPublisher inspectContainerPublisher,
    ILogger<DataExtractedConsumer> logger) : IConsumer<DataExtracted>
{
    private static readonly string[] PrefijosPuerto = ["Puerto de ", "Puerto "];

    public async Task Consume(ConsumeContext<DataExtracted> context)
    {
        var evento = context.Message;
        logger.LogInformation("DataExtracted recibido: shipment_id={ShipmentId}, container_number={ContainerNumber}",
            evento.ShipmentId, evento.ContainerNumber);

        var terminales = await unitOfWork.Terminales.Query().ToListAsync(context.CancellationToken);
        var idTerminalLlegada = ResolveTerminalLlegadaId(evento.PortOfDestination, terminales);

        var container = new Container
        {
            IdCargaDocumentos = evento.ShipmentId,
            NumeroContainer = evento.ContainerNumber,
            PuertoDestino = evento.PortOfDestination,
            NumeroBl = evento.BlNumber,
            NombreForwarder = evento.ForwarderName,
            FechaLlegadaEstimada = evento.Eta,
            DiasLibresDemurrage = evento.DemurrageFreeDays ?? 0,
            DiasLibresDetention = evento.DetentionFreeDays ?? 0,
            IdTerminalLlegada = idTerminalLlegada,
        };

        await unitOfWork.Containers.AddAsync(container, context.CancellationToken);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await unitOfWork.Hitos.AddAsync(new Hito
        {
            IdContainer = container.Id,
            Descripcion = $"Datos extraídos por IA (confianza {evento.Confidence:0.00}, estado {evento.ExtractionStatus})",
            Origen = "IA",
            CreadoPor = null,
            FechaOcurrencia = DateTimeOffset.UtcNow,
        }, context.CancellationToken);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        var inspectionResult = await inspectContainerPublisher.PublishAsync(container.Id, context.CancellationToken);
        if (!inspectionResult.IsSuccess)
        {
            logger.LogInformation("No se disparó InspectContainer para container_id={ContainerId}: {Error}",
                container.Id, inspectionResult.Error);
        }
    }

    private int? ResolveTerminalLlegadaId(string? puertoDestino, List<Terminal> terminales)
    {
        if (string.IsNullOrWhiteSpace(puertoDestino))
        {
            logger.LogInformation("DataExtracted sin PuertoDestino, no se puede inferir terminal de llegada");
            return null;
        }

        var ciudad = puertoDestino;
        foreach (var prefijo in PrefijosPuerto)
        {
            if (ciudad.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            {
                ciudad = ciudad[prefijo.Length..];
                break;
            }
        }
        ciudad = ciudad.Trim();

        var candidatos = terminales
            .Where(t => t.NombreTerminal.Contains(ciudad, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidatos.Count == 0)
        {
            logger.LogWarning("No se encontró terminal para PuertoDestino='{PuertoDestino}'", puertoDestino);
            return null;
        }

        if (candidatos.Count > 1)
        {
            logger.LogWarning(
                "PuertoDestino='{PuertoDestino}' coincide con {Count} terminales ({Codigos}); se usa el primero como heurística provisional",
                puertoDestino, candidatos.Count, string.Join(",", candidatos.Select(c => c.CodigoTerminal)));
        }

        return candidatos[0].Id;
    }
}
