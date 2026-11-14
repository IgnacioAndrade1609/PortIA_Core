namespace PortIA.Core.Services.Component.Terminales.Mappers;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Component.Terminales.DTOs;

public static class TerminalMapper
{
    public static Terminal ToEntity(this CreateTerminalRequest request) => new()
    {
        CodigoTerminal = NormalizarCodigo(request.CodigoTerminal),
        NombreTerminal = request.NombreTerminal.Trim(),
        IdPais = request.IdPais,
        ConfigScraper = Normalizar(request.ConfigScraper),
        Vigente = true
    };

    public static void Apply(this Terminal terminal, UpdateTerminalRequest request)
    {
        terminal.CodigoTerminal = NormalizarCodigo(request.CodigoTerminal);
        terminal.NombreTerminal = request.NombreTerminal.Trim();
        terminal.IdPais = request.IdPais;
        terminal.ConfigScraper = Normalizar(request.ConfigScraper);
    }

    public static TerminalDto ToDto(this Terminal terminal, string? nombrePais) => new()
    {
        Id = terminal.Id,
        CodigoTerminal = terminal.CodigoTerminal,
        NombreTerminal = terminal.NombreTerminal,
        IdPais = terminal.IdPais,
        Pais = nombrePais,
        ConfigScraper = terminal.ConfigScraper,
        Vigente = terminal.Vigente
    };

    public static TerminalDto ToDto(this Terminal terminal) => terminal.ToDto(terminal.Pais?.NombrePais);

    public static PaisDto ToDto(this Pais pais) => new()
    {
        Id = pais.Id,
        NombrePais = pais.NombrePais
    };

    public static string NormalizarCodigo(string codigo) => codigo.Trim().ToUpperInvariant();

    public static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
