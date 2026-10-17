namespace PortIA.Core.Infrastructure.Data.Entities;

public class CargaDocumentos
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "PENDIENTE";
    public DateTimeOffset CreacionFecha { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdateAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Vigente { get; set; } = true;

    public Usuario Usuario { get; set; } = null!;
    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
    public ICollection<Container> Containers { get; set; } = new List<Container>();
}
