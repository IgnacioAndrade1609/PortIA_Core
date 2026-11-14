namespace PortIA.Core.Services.Component.Teams.DTOs;

public record AnalystDocumentResponse(
    int Id,
    string TipoDocumento,
    DateTimeOffset CreateAt,
    string EstadoExtraccion,
    float? Confidence,
    int AnalystId,
    string AnalystName,
    int? ContainerId,
    string? ContainerNumber,
    string DownloadUrl
);
