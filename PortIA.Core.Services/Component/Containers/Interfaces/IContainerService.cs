namespace PortIA.Core.Services.Component.Containers.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Containers.DTOs;

public interface IContainerService
{
    Task<Result<List<ContainerSummaryResponse>>> GetContainersAsync(
        int userId,
        string userRole,
        string? search,
        string? riskLevel,
        int? shippingLineId,
        int? originTerminalId,
        int? destinationTerminalId,
        int? assignedTo,
        bool? managed,
        bool? needsReview,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct);

    Task<Result> TriggerInspectionAsync(int containerId, CancellationToken ct);

    Task<Result<IReadOnlyList<HitoResponse>>> GetHitosAsync(int containerId, int userId, CancellationToken ct);
}
