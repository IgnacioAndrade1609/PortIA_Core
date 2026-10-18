namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ShipmentSummaryProjection(
    int Id,
    string Status,
    int DocumentCount,
    int ContainerCount,
    DateTimeOffset CreacionFecha,
    DateTimeOffset UpdateAt
);
