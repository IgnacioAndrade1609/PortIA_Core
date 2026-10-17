namespace PortIA.Core.Infrastructure.Data.Entities;

public class Canal
{
    public int Id { get; set; }
    public string Tipo { get; set; } = null!;
    public bool Vigente { get; set; } = true;

    public ICollection<LogNotificacion> LogsNotificacion { get; set; } = new List<LogNotificacion>();
}
