namespace PortIA.Core.Services.Component.Terminales.DTOs;

public record UpdateTerminalRequest(
    string CodigoTerminal,
    string NombreTerminal,
    int? IdPais,
    string? ConfigScraper
);
