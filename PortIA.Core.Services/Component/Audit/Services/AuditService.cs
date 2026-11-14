namespace PortIA.Core.Services.Component.Audit.Services;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
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
    public async Task<Result<PagedResult<AuditLogResponse>>> GetPagedAsync(
        AuditQueryFilter filter, int callerUserId, string callerRole, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(filter, ct);
        if (!validation.IsValid)
            return Result<PagedResult<AuditLogResponse>>.ValidationError(validation.ToDictionary());

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

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.CreateAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.CreateAt <= filter.ToDate.Value);

        var totalItems = await query.LongCountAsync(ct);

        var entidades = await query
            .OrderByDescending(x => x.CreateAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        var items = entidades.Select(x => x.ToResponse()).ToList();

        var totalPages = filter.PageSize > 0
            ? (int)Math.Ceiling(totalItems / (double)filter.PageSize)
            : 1;

        var response = new PagedResult<AuditLogResponse>(
            items, filter.Page, filter.PageSize, totalItems, totalPages == 0 ? 1 : totalPages);

        return Result<PagedResult<AuditLogResponse>>.Success(response);
    }
}
