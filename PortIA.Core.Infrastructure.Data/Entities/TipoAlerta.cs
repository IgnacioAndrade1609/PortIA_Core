namespace PortIA.Core.Infrastructure.Data.Entities;

public class TipoAlerta
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = null!;
    public bool Vigente { get; set; } = true;

    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
}
