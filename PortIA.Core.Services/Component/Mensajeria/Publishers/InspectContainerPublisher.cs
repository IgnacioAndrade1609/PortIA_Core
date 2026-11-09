namespace PortIA.Core.Services.Component.Mensajeria.Publishers;

using MassTransit;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;

public class InspectContainerPublisher(
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint,
    ILogger<InspectContainerPublisher> logger) : IInspectContainerPublisher
{
    public async Task<Result> PublishAsync(int containerId, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);
        if (container is null)
        {
            logger.LogWarning("PublishAsync: container {ContainerId} no existe", containerId);
            return Result.NotFound($"No existe el contenedor {containerId}.");
        }

        if (container.IdTerminalLlegada is null)
        {
            logger.LogInformation("PublishAsync: container {ContainerId} sin terminal de llegada asignado", containerId);
            return Result.BusinessError("El contenedor no tiene un terminal de llegada asignado todavía.");
        }

        var terminal = await unitOfWork.Terminales.GetByIdAsync(container.IdTerminalLlegada.Value, ct);
        if (terminal is null)
        {
            logger.LogWarning("PublishAsync: terminal {TerminalId} no existe", container.IdTerminalLlegada);
            return Result.BusinessError($"El terminal {container.IdTerminalLlegada} no existe.");
        }

        var evento = new InspectContainer(
            ContainerId: container.Id,
            ContainerNumber: container.NumeroContainer,
            TerminalCode: terminal.CodigoTerminal,
            TerminalConfig: new Dictionary<string, object> { ["url"] = terminal.ConfigScraper ?? string.Empty }
        );

        await publishEndpoint.Publish(evento, ct);

        logger.LogInformation("InspectContainer enviado: container_id={ContainerId}, terminal_code={TerminalCode}",
            evento.ContainerId, evento.TerminalCode);

        return Result.Success();
    }
}
