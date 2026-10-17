namespace PortIA.Core.Infrastructure.Data.Entities;

public class Accion
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = null!;

    public ICollection<LogAuditoria> LogsAuditoria { get; set; } = new List<LogAuditoria>();
}
