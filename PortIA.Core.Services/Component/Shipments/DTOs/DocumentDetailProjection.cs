namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record DocumentDetailProjection(
    int Id,
    string TipoDocumento,
    string StorageUrl,
    float? Confidence,
    string EstadoExtraccion,
    bool ContieneDatosPersonales,
    DateTimeOffset? RetencionHasta
);
