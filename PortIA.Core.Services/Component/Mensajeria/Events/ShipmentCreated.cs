namespace PortIA.Core.Services.Component.Mensajeria.Events;

public record ShipmentCreated(
    int ShipmentId,
    int DocumentId,
    string DocumentUrl,
    string DocumentType
);
