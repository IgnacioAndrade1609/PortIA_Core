namespace PortIA.Core.Services.Component.Navieras.DTOs;

public record UpdateNavieraRequest(
    string NombreNaviera,
    string? CodigoNaviera,
    string? Email,
    string? Telefono
);
