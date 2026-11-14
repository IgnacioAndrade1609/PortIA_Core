namespace PortIA.Core.Services.Component.Terminales.DTOs;

public class TerminalDto
{
    public int Id { get; set; }
    public string CodigoTerminal { get; set; } = null!;
    public string NombreTerminal { get; set; } = null!;
    public int? IdPais { get; set; }
    public string? Pais { get; set; }
    public string? ConfigScraper { get; set; }
    public bool Vigente { get; set; }
}

public class PaisDto
{
    public int Id { get; set; }
    public string NombrePais { get; set; } = null!;
}
