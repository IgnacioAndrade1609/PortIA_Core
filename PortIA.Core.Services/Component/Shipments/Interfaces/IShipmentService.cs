namespace PortIA.Core.Services.Component.Shipments.Interfaces;

using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Shipments.DTOs;

public interface IShipmentService
{
    Task<Result<ShipmentCreatedResponse>> CreateShipmentAsync(
        IReadOnlyList<ShipmentFileInput> files,
        int userId,
        CancellationToken ct);

    Task<Result<PagedResult<ShipmentSummaryResponse>>> GetShipmentsAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        string? sort,
        CancellationToken ct);

    Task<Result<ShipmentDetailResponse>> GetShipmentByIdAsync(int id, int userId, CancellationToken ct);
}
