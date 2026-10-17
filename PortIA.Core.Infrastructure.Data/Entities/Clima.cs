namespace PortIA.Core.Infrastructure.Data.Entities;

public class Clima
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = null!;

    public ICollection<Container> Containers { get; set; } = new List<Container>();
}
