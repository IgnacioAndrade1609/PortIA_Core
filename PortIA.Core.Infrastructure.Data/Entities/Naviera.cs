namespace PortIA.Core.Infrastructure.Data.Entities;

public class Naviera
{
    public int Id { get; set; }
    public string NombreNaviera { get; set; } = null!;
    public string? CodigoNaviera { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Vigente { get; set; } = true;

    public ICollection<Container> Containers { get; set; } = new List<Container>();
}
