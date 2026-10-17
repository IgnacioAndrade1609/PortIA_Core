namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ShipmentDetailDto(
    int Id,
    string Status,
    IReadOnlyList<DocumentDetailDto> Documents,
    IReadOnlyList<object> Containers
);

public record DocumentDetailDto(
    int Id,
    string Type,
    string FileName,
    float? Confidence,
    string ExtractionStatus,
    bool ContainsPersonalData,
    DateTime? RetainUntil
);