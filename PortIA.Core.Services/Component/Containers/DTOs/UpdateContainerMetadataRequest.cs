namespace PortIA.Core.Services.Component.Containers.DTOs;

public record UpdateContainerMetadataRequest(
    string? Naviera,
    string? TerminalOrigen,
    string? TerminalLlegada,
    string? NumeroBl,
    string? NombreForwarder,
    DateTimeOffset? Eta
);
