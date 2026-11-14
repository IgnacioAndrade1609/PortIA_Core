namespace PortIA.Core.Services.Component.Navieras.DTOs;

public record CreateNavieraRequest(
    string NombreNaviera,
    string? CodigoNaviera,
    string? Email,
    string? Telefono
);
