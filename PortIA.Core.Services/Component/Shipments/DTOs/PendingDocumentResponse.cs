namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record PendingDocumentResponse(
    int DocumentId,
    int ShipmentId,
    float? Confidence,
    string? ContainerNumberSugerido,
    string? BlNumberSugerido,
    string? ShippingLineSugerido,
    string? PortOfDestinationSugerido,
    DateTimeOffset? EtaSugerido,
    int? DemurrageFreeDaysSugerido,
    int? DetentionFreeDaysSugerido,
    string? ForwarderNameSugerido,
    DateTimeOffset CreateAt
);
