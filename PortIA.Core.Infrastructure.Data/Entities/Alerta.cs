namespace PortIA.Core.Infrastructure.Data.Entities;

public class Alerta
{
    public int Id { get; set; }
    public int IdContainer { get; set; }
    public int IdTipoAlerta { get; set; }
    public int IdRiesgo { get; set; }
    public string Mensaje { get; set; } = null!;
    public bool Leida { get; set; }
    public int? UsuarioLector { get; set; }
    public DateTimeOffset? FechaLectura { get; set; }
    public DateTimeOffset CreateAt { get; set; } = DateTimeOffset.UtcNow;

    public Container Container { get; set; } = null!;
    public TipoAlerta TipoAlerta { get; set; } = null!;
    public Riesgo Riesgo { get; set; } = null!;
    public Usuario? UsuarioLectorNavigation { get; set; }

    public ICollection<LogNotificacion> LogsNotificacion { get; set; } = new List<LogNotificacion>();
}
