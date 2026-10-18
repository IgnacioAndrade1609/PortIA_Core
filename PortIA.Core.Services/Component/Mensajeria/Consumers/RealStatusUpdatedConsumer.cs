using MassTransit;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Mensajeria.Events;

namespace PortIA.Core.Services.Component.Mensajeria.Consumers;

public class RealStatusUpdatedConsumer(IUnitOfWork unitOfWork) : IConsumer<RealStatusUpdated>
{
    public async Task Consume(ConsumeContext<RealStatusUpdated> context)
    {
        var evento = context.Message;

        var container = await unitOfWork.Containers.GetByIdAsync(evento.ContainerId, context.CancellationToken);

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

        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
