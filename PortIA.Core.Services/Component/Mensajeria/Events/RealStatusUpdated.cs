namespace PortIA.Core.Services.Component.Mensajeria.Events;

/// <summary>
/// Publica: Agente Inspector. Consume: Core (RealStatusUpdatedConsumer).
/// Contrato completo en PortIA_Agentes/EVENTOS.md.
/// </summary>
public record RealStatusUpdated(
    int ContainerId,
    string ContainerNumber,
    DateTimeOffset? RealArrivalDate,
    string PortStatus,
    bool WeatherAlert,
    Dictionary<string, object>? WeatherData,
    string ScraperSource,
    DateTimeOffset ScrapedAt
);
