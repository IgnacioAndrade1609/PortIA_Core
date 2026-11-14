namespace PortIA.Core.Services.Component.Navieras.DTOs;

public class NavieraDto
{
    public int Id { get; set; }
    public string NombreNaviera { get; set; } = null!;
    public string? CodigoNaviera { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Vigente { get; set; }
}
