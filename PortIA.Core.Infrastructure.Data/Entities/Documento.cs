namespace PortIA.Core.Infrastructure.Data.Entities;

public class Documento
{
    public int Id { get; set; }
    public int IdCargaDocumentos { get; set; }
    public string TipoDocumento { get; set; } = null!;
    public string StorageUrl { get; set; } = null!;
    public string HuellaHash { get; set; } = null!;
    public float? Confidence { get; set; }
    public string EstadoExtraccion { get; set; } = "PENDIENTE";
    public string? DatosExtraidosRaw { get; set; }
    public bool ContieneDatosPersonales { get; set; } = true;
    public DateTimeOffset? RetencionHasta { get; set; }
    public DateTimeOffset CreateAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Vigente { get; set; } = true;

    public CargaDocumentos CargaDocumentos { get; set; } = null!;
}
