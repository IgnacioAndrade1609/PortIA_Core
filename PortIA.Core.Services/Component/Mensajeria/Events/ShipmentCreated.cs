namespace PortIA.Core.Services.Component.Mensajeria.Events;

/// <summary>
/// Publica: Core. Consume: Agente Lector.
/// Contrato completo en PortIA_Agentes/EVENTOS.md.
/// </summary>
public record ShipmentCreated(
    int ShipmentId,
    int DocumentId,
    string DocumentUrl,
    string DocumentType
);
