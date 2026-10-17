using MassTransit;
using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Services.Component.Mensajeria.Events;

namespace PortIA.Core.Services.Component.Mensajeria.Consumers;

public class RealStatusUpdatedConsumer(PortIACoreDbContext db) : IConsumer<RealStatusUpdated>
{
    public async Task Consume(ConsumeContext<RealStatusUpdated> context)
    {
        var evento = context.Message;

        var container = await db.Containers
            .FirstOrDefaultAsync(c => c.Id == evento.ContainerId, context.CancellationToken);

        if (container is null)
        {
            // El contenedor no existe (evento llegó fuera de orden o con un ID inválido).
            // No hay reintento posible con otro dato, así que se descarta explícitamente
            // en vez de lanzar: lanzar mandaría el mensaje a la DLQ para un caso no recuperable.
            return;
        }

        container.FechaArriboReal = evento.RealArrivalDate;
        container.EstadoPuerto = evento.PortStatus;
        container.UpdateAt = DateTimeOffset.UtcNow;

        if (evento.RealArrivalDate is not null)
        {
            container.DeadlineDemurrage = evento.RealArrivalDate.Value.AddDays(container.DiasLibresDemurrage);
        }

        await db.SaveChangesAsync(context.CancellationToken);

        // TODO: recalcular ID_RIESGO (semáforo) y crear ALERTA si corresponde — regla de
        // negocio de la Capa 5 (sección 6 del CONTEXTO_MAESTRO), pendiente de su propia historia.
    }
}
