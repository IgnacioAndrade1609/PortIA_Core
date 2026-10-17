namespace PortIA.Core.Infrastructure.Data.Entities;

public class Terminal
{
    public int Id { get; set; }
    public string CodigoTerminal { get; set; } = null!;
    public string NombreTerminal { get; set; } = null!;
    public int? IdPais { get; set; }
    public string? ConfigScraper { get; set; }
    public bool Vigente { get; set; } = true;

    public Pais? Pais { get; set; }
    public ICollection<Container> ContainersOrigen { get; set; } = new List<Container>();
    public ICollection<Container> ContainersLlegada { get; set; } = new List<Container>();
}
