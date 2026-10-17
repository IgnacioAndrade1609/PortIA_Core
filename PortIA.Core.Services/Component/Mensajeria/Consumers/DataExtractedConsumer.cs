using MassTransit;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Component.Mensajeria.Events;

namespace PortIA.Core.Services.Component.Mensajeria.Consumers;

public class DataExtractedConsumer(PortIACoreDbContext db) : IConsumer<DataExtracted>
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

        db.Containers.Add(container);
        await db.SaveChangesAsync(context.CancellationToken);

        db.Hitos.Add(new Hito
        {
            IdContainer = container.Id,
            Descripcion = $"Datos extraídos por IA (confianza {evento.Confidence:0.00}, estado {evento.ExtractionStatus})",
            Origen = "IA",
            CreadoPor = null,
            FechaOcurrencia = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(context.CancellationToken);

        // TODO: publicar InspectContainer. Pendiente de la decisión abierta #3 del
        // CONTEXTO_MAESTRO: falta ID_TERMINAL en CONTAINER y definir quién lo asigna
        // (¿lo extrae el Lector a partir del puerto de destino, o lo elige el usuario
        // manualmente en el frontend?). Sin eso no hay terminal_code que publicar.
    }
}
