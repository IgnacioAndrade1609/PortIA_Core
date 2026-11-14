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
        int? jefeId,
        bool? managed,
        bool? needsReview,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct);

    Task<Result> TriggerInspectionAsync(int containerId, CancellationToken ct);

    Task<Result<IReadOnlyList<HitoResponse>>> GetHitosAsync(int containerId, int userId, string userRole, CancellationToken ct);

    Task<Result> MarkRetiroAsync(int containerId, int userId, CancellationToken ct);

    Task<Result> MarkDevolucionVaciaAsync(int containerId, int userId, CancellationToken ct);

    Task<Result> ConfirmarArriboRealAsync(int containerId, int userId, DateTimeOffset fechaArriboReal, CancellationToken ct);

    Task<Result> UpdateDiasLibresAsync(int containerId, int userId, int diasLibresDemurrage, int diasLibresDetention, CancellationToken ct);

    Task<Result> UpdateDatosExtraidosAsync(int containerId, int userId, UpdateContainerMetadataRequest request, CancellationToken ct);
}
