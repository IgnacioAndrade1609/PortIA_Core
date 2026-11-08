namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record ShipmentFileInput(
    Stream Content,
    string FileName,
    string ContentType,
    long Length
);
