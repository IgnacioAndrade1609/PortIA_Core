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
    }
}
