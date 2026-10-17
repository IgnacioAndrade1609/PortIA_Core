namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ShipmentSummaryDto(
    int Id,
    string Status,
    int DocumentCount,
    int ContainerCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);