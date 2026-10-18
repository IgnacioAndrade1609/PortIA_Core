namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ShipmentCreatedResponse(
    int ShipmentId,
    int DocumentId,
    string Status,
    DateTime CreatedAt
);
