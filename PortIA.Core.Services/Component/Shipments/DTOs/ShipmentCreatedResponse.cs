namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ShipmentCreatedResponse(
    int ShipmentId,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<UploadedDocumentResult> Documents
);
