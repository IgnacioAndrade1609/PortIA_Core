namespace PortIA.Core.Infrastructure.Data.Entities;

public class Hito
{
    public int Id { get; set; }
    public int IdContainer { get; set; }
    public string Descripcion { get; set; } = null!;
    public string Origen { get; set; } = null!;
    public int? CreadoPor { get; set; }
    public DateTimeOffset FechaOcurrencia { get; set; }

    public Container Container { get; set; } = null!;
    public Usuario? CreadoPorUsuario { get; set; }
}
