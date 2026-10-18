namespace PortIA.Core.Services.Component.Shipments.Mappers;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Component.Shipments.DTOs;

public static class ShipmentMapper
{
    private const float LowConfidenceThreshold = 0.7f;

    public static ShipmentCreatedResponse ToCreatedResponse(this CargaDocumentos carga, Documento documento) =>
        new(
            ShipmentId: carga.Id,
            DocumentId: documento.Id,
            Status: carga.Status,
            CreatedAt: carga.CreacionFecha.UtcDateTime
        );

    public static ShipmentSummaryResponse ToSummaryResponse(this ShipmentSummaryProjection projection) =>
        new(
            projection.Id,
            projection.Status,
            projection.DocumentCount,
            projection.ContainerCount,
            projection.CreacionFecha.UtcDateTime,
            projection.UpdateAt.UtcDateTime
        );

    public static DocumentDetailResponse ToDetailResponse(this DocumentDetailProjection projection) =>
        new(
            projection.Id,
            projection.TipoDocumento,
            Path.GetFileName(projection.StorageUrl),
            projection.Confidence,
            projection.Confidence.HasValue && projection.Confidence.Value < LowConfidenceThreshold
                ? "NEEDS_REVIEW"
                : projection.EstadoExtraccion,
            projection.ContieneDatosPersonales,
            projection.RetencionHasta?.UtcDateTime
        );

    public static ShipmentDetailResponse ToDetailResponse(this CargaDocumentos carga, IReadOnlyList<DocumentDetailResponse> documents) =>
        new(
            Id: carga.Id,
            Status: carga.Status,
            Documents: documents,
            Containers: new List<object>()
        );
}
