namespace PortIA.Core.Services.Component.Audit.Services;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Audit.DTOs;
using PortIA.Core.Services.Component.Audit.Interfaces;
using PortIA.Core.Services.Component.Audit.Mappers;

public class AuditService(
    IUnitOfWork unitOfWork,
    IValidator<AuditQueryFilter> validator) : IAuditService
{
    private const int LimiteAnalisis = 5000;
    private const string OrigenSistema = "SISTEMA";
    private const string OrigenUsuario = "USUARIO";

    public async Task<Result<PagedResult<AuditLogResponse>>> GetPagedAsync(
        AuditQueryFilter filter, int callerUserId, string callerRole, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(filter, ct);
        if (!validation.IsValid)
            return Result<PagedResult<AuditLogResponse>>.ValidationError(validation.ToDictionary());

        var query = await BuildQueryAsync(filter, callerUserId, callerRole, ct);

        if (filter.SoloSensibles)
        {
            var candidatos = await query
                .OrderByDescending(x => x.CreateAt)
                .Take(LimiteAnalisis)
                .Select(x => new { x.Id, Entidad = x.NombreEntidad, Accion = x.Accion.Descripcion, x.ValoresAnteriores, x.ValoresNuevos })
                .ToListAsync(ct);

            var idsSensibles = candidatos
                .Where(c => AuditSensitivity.Clasificar(c.Entidad, c.Accion, c.ValoresAnteriores, c.ValoresNuevos) is not null)
                .Select(c => c.Id)
                .ToList();

            query = query.Where(x => idsSensibles.Contains(x.Id));
        }

        var totalItems = await query.LongCountAsync(ct);

        var entidades = await query
            .OrderByDescending(x => x.CreateAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        var contenedores = await ResolverContenedoresAsync(entidades, ct);

        var items = entidades.Select(x =>
        {
            var response = x.ToResponse();
            contenedores.TryGetValue(x.Id, out var contenedor);
            return response with
            {
                ContenedorId = contenedor.Id,
                ContenedorNumero = contenedor.Numero,
                Sensibilidad = AuditSensitivity.Clasificar(x.NombreEntidad, x.Accion.Descripcion, x.ValoresAnteriores, x.ValoresNuevos)
            };
        }).ToList();

        var totalPages = filter.PageSize > 0
            ? (int)Math.Ceiling(totalItems / (double)filter.PageSize)
            : 1;

        var respuesta = new PagedResult<AuditLogResponse>(
            items, filter.Page, filter.PageSize, totalItems, totalPages == 0 ? 1 : totalPages);

        return Result<PagedResult<AuditLogResponse>>.Success(respuesta);
    }

    public async Task<Result<AuditSummaryResponse>> GetSummaryAsync(
        AuditQueryFilter filter, int callerUserId, string callerRole, CancellationToken ct)
    {
        if (!callerRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return Result<AuditSummaryResponse>.Unauthorized("Solo un administrador puede ver el resumen de auditoría.");

        var validation = await validator.ValidateAsync(filter, ct);
        if (!validation.IsValid)
            return Result<AuditSummaryResponse>.ValidationError(validation.ToDictionary());

        var query = await BuildQueryAsync(filter, callerUserId, callerRole, ct);

        var filas = await query
            .OrderByDescending(x => x.CreateAt)
            .Take(LimiteAnalisis + 1)
            .Select(x => new { x.Responsable, Entidad = x.NombreEntidad, Accion = x.Accion.Descripcion, x.ValoresAnteriores, x.ValoresNuevos })
            .ToListAsync(ct);

        var truncado = filas.Count > LimiteAnalisis;
        if (truncado) filas = filas.Take(LimiteAnalisis).ToList();

        var sensibles = filas
            .Select(f => AuditSensitivity.Clasificar(f.Entidad, f.Accion, f.ValoresAnteriores, f.ValoresNuevos))
            .Where(t => t is not null)
            .GroupBy(t => t!)
            .Select(g => new AuditSensitiveCount(g.Key, g.Count()))
            .OrderByDescending(c => c.Cantidad)
            .ToList();

        var resumen = new AuditSummaryResponse(
            TotalEventos: filas.Count,
            UsuariosActivos: filas.Where(f => f.Responsable.HasValue).Select(f => f.Responsable!.Value).Distinct().Count(),
            EventosDeSistema: filas.Count(f => !f.Responsable.HasValue),
            AccionesSensibles: sensibles.Sum(s => s.Cantidad),
            SensiblesPorTipo: sensibles,
            ResultadoTruncado: truncado);

        return Result<AuditSummaryResponse>.Success(resumen);
    }

    private async Task<IQueryable<LogAuditoria>> BuildQueryAsync(
        AuditQueryFilter filter, int callerUserId, string callerRole, CancellationToken ct)
    {
        var effectiveResponsableId = callerRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ? filter.ResponsableId
            : callerUserId;

        var query = unitOfWork.LogsAuditoria.Query();

        if (effectiveResponsableId.HasValue)
            query = query.Where(x => x.Responsable == effectiveResponsableId.Value);

        if (!string.IsNullOrWhiteSpace(filter.NombreEntidad))
            query = query.Where(x => x.NombreEntidad == filter.NombreEntidad);

        if (!string.IsNullOrWhiteSpace(filter.Accion))
            query = query.Where(x => x.Accion.Descripcion == filter.Accion);

        if (string.Equals(filter.Origen, OrigenSistema, StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Responsable == null);
        else if (string.Equals(filter.Origen, OrigenUsuario, StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Responsable != null);

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.CreateAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.CreateAt <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.ContainerNumber))
        {
            var texto = filter.ContainerNumber.Trim().ToLower();

            var containerIds = await unitOfWork.Containers.Query()
                .Where(c => c.NumeroContainer.ToLower().Contains(texto))
                .Select(c => c.Id)
                .ToListAsync(ct);

            var alertaIds = containerIds.Count == 0
                ? new List<int>()
                : await unitOfWork.Alertas.Query()
                    .Where(a => containerIds.Contains(a.IdContainer))
                    .Select(a => a.Id)
                    .ToListAsync(ct);

            var notificacionIds = alertaIds.Count == 0
                ? new List<int>()
                : await unitOfWork.LogsNotificacion.Query()
                    .Where(l => alertaIds.Contains(l.IdAlerta))
                    .Select(l => l.Id)
                    .ToListAsync(ct);

            query = query.Where(x =>
                (x.NombreEntidad == "Container" && containerIds.Contains(x.IdEntidad)) ||
                (x.NombreEntidad == "Alerta" && alertaIds.Contains(x.IdEntidad)) ||
                (x.NombreEntidad == "Notificacion" && notificacionIds.Contains(x.IdEntidad)));
        }

        return query;
    }

    private async Task<Dictionary<int, (int? Id, string? Numero)>> ResolverContenedoresAsync(
        IReadOnlyList<LogAuditoria> entidades, CancellationToken ct)
    {
        var contenedorPorLog = new Dictionary<int, int>();

        var alertaPorNotificacion = new Dictionary<int, int>();
        var idsNotificacion = entidades.Where(e => e.NombreEntidad == "Notificacion").Select(e => e.IdEntidad).Distinct().ToList();
        if (idsNotificacion.Count > 0)
        {
            alertaPorNotificacion = await unitOfWork.LogsNotificacion.Query()
                .Where(l => idsNotificacion.Contains(l.Id))
                .Select(l => new { l.Id, l.IdAlerta })
                .ToDictionaryAsync(x => x.Id, x => x.IdAlerta, ct);
        }

        var idsAlerta = entidades.Where(e => e.NombreEntidad == "Alerta").Select(e => e.IdEntidad)
            .Concat(alertaPorNotificacion.Values)
            .Distinct()
            .ToList();

        var contenedorPorAlerta = new Dictionary<int, int>();
        if (idsAlerta.Count > 0)
        {
            contenedorPorAlerta = await unitOfWork.Alertas.Query()
                .Where(a => idsAlerta.Contains(a.Id))
                .Select(a => new { a.Id, a.IdContainer })
                .ToDictionaryAsync(x => x.Id, x => x.IdContainer, ct);
        }

        foreach (var e in entidades)
        {
            if (e.NombreEntidad == "Container")
            {
                contenedorPorLog[e.Id] = e.IdEntidad;
            }
            else if (e.NombreEntidad == "Alerta" && contenedorPorAlerta.TryGetValue(e.IdEntidad, out var porAlerta))
            {
                contenedorPorLog[e.Id] = porAlerta;
            }
            else if (e.NombreEntidad == "Notificacion"
                     && alertaPorNotificacion.TryGetValue(e.IdEntidad, out var alertaId)
                     && contenedorPorAlerta.TryGetValue(alertaId, out var porNotificacion))
            {
                contenedorPorLog[e.Id] = porNotificacion;
            }
        }

        var numeros = new Dictionary<int, string>();
        var idsContenedor = contenedorPorLog.Values.Distinct().ToList();
        if (idsContenedor.Count > 0)
        {
            numeros = await unitOfWork.Containers.Query()
                .Where(c => idsContenedor.Contains(c.Id))
                .Select(c => new { c.Id, c.NumeroContainer })
                .ToDictionaryAsync(x => x.Id, x => x.NumeroContainer, ct);
        }

        return contenedorPorLog.ToDictionary(
            kv => kv.Key,
            kv => ((int?)kv.Value, numeros.TryGetValue(kv.Value, out var numero) ? numero : (string?)null));
    }
}
