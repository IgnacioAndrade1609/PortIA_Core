namespace PortIA.Core.Services.Component.Mensajeria.Events;

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
