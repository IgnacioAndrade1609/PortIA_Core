namespace PortIA.Core.Infrastructure.Data.Entities;

public class LogAuditoria
{
    public int Id { get; set; }
    public string NombreEntidad { get; set; } = null!;
    public int IdEntidad { get; set; }
    public int IdAccion { get; set; }
    public int? Responsable { get; set; }
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }
    public DateTimeOffset CreateAt { get; set; } = DateTimeOffset.UtcNow;

    public Accion Accion { get; set; } = null!;
    public Usuario? ResponsableUsuario { get; set; }
}
