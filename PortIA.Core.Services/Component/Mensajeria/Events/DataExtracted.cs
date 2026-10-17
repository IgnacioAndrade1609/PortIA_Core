namespace PortIA.Core.Services.Component.Mensajeria.Events;

/// <summary>
/// Publica: Agente Lector. Consume: Core (DataExtractedConsumer).
/// Contrato completo en PortIA_Agentes/EVENTOS.md. extraction_status: "OK" | "NEEDS_REVIEW" | "ERROR".
/// </summary>
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
