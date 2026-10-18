namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record CreateShipmentRequest(
    string FileName,
    string ContentType,
    long FileSize
);
