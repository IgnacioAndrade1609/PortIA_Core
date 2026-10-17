namespace PortIA.Core.Infrastructure.Data.Entities;

public class LogNotificacion
{
    public int Id { get; set; }
    public int IdAlerta { get; set; }
    public int IdCanal { get; set; }
    public string Destinatario { get; set; } = null!;
    public string Estado { get; set; } = "PENDIENTE_APROBACION";
    public int? AprobadoPor { get; set; }
    public DateTimeOffset CreateAt { get; set; } = DateTimeOffset.UtcNow;

    public Alerta Alerta { get; set; } = null!;
    public Canal Canal { get; set; } = null!;
    public Usuario? AprobadoPorUsuario { get; set; }
}
