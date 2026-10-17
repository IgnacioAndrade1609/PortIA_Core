namespace PortIA.Core.Services.Component.Shipments.Interfaces;

using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Component.Shipments.DTOs;

public interface IShipmentService
{
    Task<ShipmentCreatedResponseDto> CreateShipmentAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        int userId,
        CancellationToken ct);

    Task<PagedResult<ShipmentSummaryDto>> GetShipmentsAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        string? sort,
        CancellationToken ct);

    Task<ShipmentDetailDto?> GetShipmentByIdAsync(int id, int userId, CancellationToken ct);
}