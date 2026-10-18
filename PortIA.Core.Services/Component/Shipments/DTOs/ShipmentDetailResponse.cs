namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ShipmentDetailResponse(
    int Id,
    string Status,
    IReadOnlyList<DocumentDetailResponse> Documents,
    IReadOnlyList<object> Containers
);

public record DocumentDetailResponse(
    int Id,
    string Type,
    string FileName,
    float? Confidence,
    string ExtractionStatus,
    bool ContainsPersonalData,
    DateTime? RetainUntil
);
