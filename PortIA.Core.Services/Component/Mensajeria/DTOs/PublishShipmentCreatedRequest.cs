namespace PortIA.Core.Services.Component.Mensajeria.DTOs;

public record PublishShipmentCreatedRequest(
    int ShipmentId,
    int DocumentId,
    string DocumentUrl,
    string DocumentType
);
