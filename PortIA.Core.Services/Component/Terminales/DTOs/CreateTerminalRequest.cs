namespace PortIA.Core.Services.Component.Terminales.DTOs;

public record CreateTerminalRequest(
    string CodigoTerminal,
    string NombreTerminal,
    int? IdPais,
    string? ConfigScraper
);
