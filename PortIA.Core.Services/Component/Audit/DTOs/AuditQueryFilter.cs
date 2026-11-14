namespace PortIA.Core.Services.Component.Audit.DTOs;

public class AuditQueryFilter
{
    public int? ResponsableId { get; set; }
    public string? NombreEntidad { get; set; }
    public string? Accion { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
