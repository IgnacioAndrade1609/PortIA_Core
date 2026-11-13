namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ValidateDocumentRequest(
    string ContainerNumber,
    string? BlNumber,
    string? ShippingLine,
    string? PortOfDestination,
    DateTimeOffset? Eta,
    int? DemurrageFreeDays,
    int? DetentionFreeDays,
    string? ForwarderName
);
