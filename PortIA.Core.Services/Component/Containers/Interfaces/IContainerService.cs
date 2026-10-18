namespace PortIA.Core.Services.Component.Containers.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Containers.DTOs;

public interface IContainerService
{
    Task<Result> TriggerInspectionAsync(int containerId, CancellationToken ct);

    Task<Result<IReadOnlyList<HitoResponse>>> GetHitosAsync(int containerId, int userId, CancellationToken ct);
}
