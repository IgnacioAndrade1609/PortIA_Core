using MassTransit;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Mensajeria.Events;

namespace PortIA.Core.Services.Component.Mensajeria.Consumers;

public class DataExtractedConsumer(IUnitOfWork unitOfWork) : IConsumer<DataExtracted>
{
    public async Task Consume(ConsumeContext<DataExtracted> context)
    {
        var evento = context.Message;

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
    }
}
