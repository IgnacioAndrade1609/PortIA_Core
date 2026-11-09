namespace PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;

using PortIA.Core.Services.Common.Results;

public interface IInspectContainerPublisher
{
    Task<Result> PublishAsync(int containerId, CancellationToken ct);
}
