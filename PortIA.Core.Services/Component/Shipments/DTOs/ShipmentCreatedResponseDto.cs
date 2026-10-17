namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ShipmentCreatedResponseDto(
    int ShipmentId,
    int DocumentId,
    string Status,
    DateTime CreatedAt
);