namespace PortIA.Core.Infrastructure.Data.Entities;

public class Riesgo
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = null!;
    public bool Vigente { get; set; } = true;

    public ICollection<Container> Containers { get; set; } = new List<Container>();
    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
}
