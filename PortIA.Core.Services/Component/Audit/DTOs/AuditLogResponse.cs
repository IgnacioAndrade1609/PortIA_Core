namespace PortIA.Core.Services.Component.Audit.DTOs;

public record AuditLogResponse(
    int Id,
    DateTimeOffset CreateAt,
    string NombreEntidad,
    int IdEntidad,
    string Accion,
    int? ResponsableId,
    string? ResponsableNombre,
    string? ValoresAnteriores,
    string? ValoresNuevos
);
