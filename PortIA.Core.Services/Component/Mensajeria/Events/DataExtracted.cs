namespace PortIA.Core.Services.Component.Mensajeria.Events;

public record DataExtracted(
    int ShipmentId,
    int DocumentId,
    string DocumentType,
    string? BlNumber,
    string ContainerNumber,
    string? ShippingLine,
    string? PortOfDestination,
    DateTimeOffset? Eta,
    int? DemurrageFreeDays,
    int? DetentionFreeDays,
    string? ForwarderName,
    Dictionary<string, object> ExtractedDataRaw,
    double Confidence,
    string ExtractionStatus
);
