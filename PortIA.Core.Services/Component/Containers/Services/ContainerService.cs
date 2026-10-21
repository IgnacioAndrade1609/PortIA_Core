namespace PortIA.Core.Services.Component.Containers.Services;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Containers.DTOs;
using PortIA.Core.Services.Component.Containers.Interfaces;
using PortIA.Core.Services.Component.Containers.Mappers;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;

public class ContainerService(
    IUnitOfWork unitOfWork,
    IInspectContainerPublisher inspectContainerPublisher) : IContainerService
{
    public async Task<Result<List<ContainerSummaryResponse>>> GetContainersAsync(
        int userId,
        string userRole,
        string? search,
        string? riskLevel,
        int? shippingLineId,
        int? originTerminalId,
        int? destinationTerminalId,
        int? assignedTo,
        bool? managed,
        bool? needsReview,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct)
    {
        var query = unitOfWork.Containers.Query()
            .Include(c => c.Naviera)
            .Include(c => c.TerminalOrigen)
            .Include(c => c.TerminalLlegada)
            .Include(c => c.Asignado)
            .Include(c => c.Riesgo)
            .Where(c => c.Vigente);

        query = ApplyRoleBasedAccess(query, userId, userRole, assignedTo);
        query = ApplyFilters(query, search, riskLevel, shippingLineId, originTerminalId, destinationTerminalId, managed, needsReview, fromDate, toDate);

        var projections = await query
            .Select(c => new ContainerSummaryProjection(
                c.Id,
                c.NumeroContainer,
                c.NumeroBl,
                c.IdNaviera,
                c.Naviera != null ? c.Naviera.NombreNaviera : null,
                c.IdTerminalOrigen,
                c.TerminalOrigen != null ? c.TerminalOrigen.NombreTerminal : null,
                c.IdTerminalLlegada,
                c.TerminalLlegada != null ? c.TerminalLlegada.NombreTerminal : null,
                c.FechaLlegadaEstimada,
                c.FechaArriboReal,
                c.FechaRetiroContainer,
                c.DiasLibresDemurrage,
                c.DiasLibresDetention,
                c.DeadlineDemurrage,
                c.DeadlineDetention,
                c.Riesgo != null ? c.Riesgo.Descripcion : null,
                false, 
                false, 
                c.EstadoPuerto,
                c.AsignadoA,
                c.Asignado != null ? c.Asignado.NombreUsuario : null,
                c.CreateAt
            ))
            .ToListAsync(ct);

        var response = projections.Select(p => p.ToSummaryResponse()).ToList();

        return Result<List<ContainerSummaryResponse>>.Success(response);
    }

    public Task<Result> TriggerInspectionAsync(int containerId, CancellationToken ct) =>
        inspectContainerPublisher.PublishAsync(containerId, ct);

    public async Task<Result<IReadOnlyList<HitoResponse>>> GetHitosAsync(int containerId, int userId, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);

        if (container is null || container.AsignadoA != userId)
            return Result<IReadOnlyList<HitoResponse>>.NotFound($"El contenedor {containerId} no existe o no pertenece al usuario.");

        var hitos = await unitOfWork.Containers.Query()
            .Where(c => c.Id == containerId)
            .SelectMany(c => c.Hitos)
            .OrderBy(h => h.FechaOcurrencia)
            .ToListAsync(ct);

        return Result<IReadOnlyList<HitoResponse>>.Success(
            hitos.Select(h => h.ToResponse()).ToList());
    }

    private static IQueryable<Container> ApplyRoleBasedAccess(
        IQueryable<Container> query, int userId, string userRole, int? requestedAssignedTo)
    {
        var normalizedRole = userRole.ToUpperInvariant();

        if (normalizedRole == "ANALIST" || normalizedRole == "ANALISTA")
        {
            return query.Where(c => c.AsignadoA == userId);
        }

        if (normalizedRole == "BOSS" || normalizedRole == "JEFE")
        {
            if (requestedAssignedTo.HasValue)
            {
                return query.Where(c => c.AsignadoA == requestedAssignedTo.Value);
            }
            return query;
        }

        if (requestedAssignedTo.HasValue)
        {
            return query.Where(c => c.AsignadoA == requestedAssignedTo.Value);
        }

        return query;
    }

    private static IQueryable<Container> ApplyFilters(
        IQueryable<Container> query,
        string? search,
        string? riskLevel,
        int? shippingLineId,
        int? originTerminalId,
        int? destinationTerminalId,
        bool? managed,
        bool? needsReview,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => 
                c.NumeroContainer.Contains(search) || 
                (c.NumeroBl != null && c.NumeroBl.Contains(search)) ||
                (c.NombreForwarder != null && c.NombreForwarder.Contains(search)));
        }

        if (shippingLineId.HasValue)
        {
            query = query.Where(c => c.IdNaviera == shippingLineId.Value);
        }

        if (originTerminalId.HasValue)
        {
            query = query.Where(c => c.IdTerminalOrigen == originTerminalId.Value);
        }

        if (destinationTerminalId.HasValue)
        {
            query = query.Where(c => c.IdTerminalLlegada == destinationTerminalId.Value);
        }

        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
            query = query.Where(c => c.CreateAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
            query = query.Where(c => c.CreateAt <= toUtc);
        }
        return query;
    }
}