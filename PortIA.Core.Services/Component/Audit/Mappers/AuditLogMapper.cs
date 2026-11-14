namespace PortIA.Core.Services.Component.Audit.Mappers;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Component.Audit.DTOs;

public static class AuditLogMapper
{
    public static AuditLogResponse ToResponse(this LogAuditoria log) => new(
        log.Id,
        log.CreateAt,
        log.NombreEntidad,
        log.IdEntidad,
        log.Accion.Descripcion,
        log.Responsable,
        log.ResponsableUsuario?.NombreUsuario,
        log.ValoresAnteriores,
        log.ValoresNuevos
    );
}
