namespace PortIA.Core.Services.Component.Containers.Services;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Containers.DTOs;
using PortIA.Core.Services.Component.Containers.Interfaces;
using PortIA.Core.Services.Component.Containers.Mappers;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;
using PortIA.Core.Services.Component.Riesgo.Configuration;
using PortIA.Core.Services.Component.Riesgo.Interfaces;

public class ContainerService(
    IUnitOfWork unitOfWork,
    IInspectContainerPublisher inspectContainerPublisher,
    IRiskEngineService riskEngineService,
    IOptions<RiskEngineOptions> riskOptions,
    ILogger<ContainerService> logger) : IContainerService
{
    private const string EstadoRetirado = "RETIRADO";
    private const string EstadoDevueltoVacio = "DEVUELTO_VACIO";
    private const string AccionActualizar = "ACTUALIZAR";

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
                c.NombreForwarder,
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
                c.EstadoContainer != null ? c.EstadoContainer.Descripcion : null,
                c.AsignadoA,
                c.Asignado != null ? c.Asignado.NombreUsuario : null,
                c.CreateAt
            ))
            .ToListAsync(ct);

        var response = projections.Select(p => p.ToSummaryResponse(riskOptions.Value)).ToList();

        return Result<List<ContainerSummaryResponse>>.Success(response);
    }

    public Task<Result> TriggerInspectionAsync(int containerId, CancellationToken ct) =>
        inspectContainerPublisher.PublishAsync(containerId, ct);

    public async Task<Result<IReadOnlyList<HitoResponse>>> GetHitosAsync(int containerId, int userId, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);

        if (container is null || container.AsignadoA != userId)
        {
            logger.LogWarning(
                "GetHitos rechazado: container {ContainerId} no existe o no pertenece a userId={UserId}",
                containerId, userId);
            return Result<IReadOnlyList<HitoResponse>>.NotFound($"El contenedor {containerId} no existe o no pertenece al usuario.");
        }

        var hitos = await unitOfWork.Containers.Query()
            .Where(c => c.Id == containerId)
            .SelectMany(c => c.Hitos)
            .OrderBy(h => h.FechaOcurrencia)
            .ToListAsync(ct);

        return Result<IReadOnlyList<HitoResponse>>.Success(
            hitos.Select(h => h.ToResponse()).ToList());
    }

    public async Task<Result> MarkRetiroAsync(int containerId, int userId, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);
        if (container is null || container.AsignadoA != userId)
        {
            logger.LogWarning(
                "MarkRetiro rechazado: container {ContainerId} no existe o no pertenece a userId={UserId}",
                containerId, userId);
            return Result.NotFound($"El contenedor {containerId} no existe o no pertenece al usuario.");
        }

        if (container.FechaRetiroContainer is not null)
        {
            return Result.BusinessError("El contenedor ya fue marcado como retirado.");
        }

        if (container.FechaArriboReal is null)
        {
            return Result.BusinessError("No se puede registrar el retiro antes de confirmar el arribo real del contenedor.");
        }

        var valoresAnteriores = BuildAuditSnapshot(container);

        var estadoRetirado = await unitOfWork.EstadosContainer.GetByDescripcionAsync(EstadoRetirado, ct)
            ?? throw new InvalidOperationException($"El catálogo ESTADO_CONTAINER no tiene una fila '{EstadoRetirado}'.");

        var ahora = DateTimeOffset.UtcNow;
        container.FechaRetiroContainer = ahora;
        container.IdEstado = estadoRetirado.Id;
        container.EstadoContainer = estadoRetirado;
        container.DeadlineDetention = ahora.AddDays(container.DiasLibresDetention);
        container.UpdateAt = ahora;

        await unitOfWork.Hitos.AddAsync(new Hito
        {
            IdContainer = container.Id,
            Descripcion = "Retiro del contenedor (gate-out) registrado manualmente por el analista.",
            Origen = "USUARIO",
            CreadoPor = userId,
            FechaOcurrencia = ahora,
        }, ct);

        await riskEngineService.RecalcularAsync(container, ct);
        await RegistrarAuditoriaAsync(container, userId, valoresAnteriores, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> MarkDevolucionVaciaAsync(int containerId, int userId, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);
        if (container is null || container.AsignadoA != userId)
        {
            logger.LogWarning(
                "MarkDevolucionVacia rechazado: container {ContainerId} no existe o no pertenece a userId={UserId}",
                containerId, userId);
            return Result.NotFound($"El contenedor {containerId} no existe o no pertenece al usuario.");
        }

        if (container.FechaRetiroContainer is null)
        {
            return Result.BusinessError("No se puede registrar la devolución vacía antes del retiro del contenedor.");
        }

        if (container.EstadoContainer?.Descripcion == EstadoDevueltoVacio)
        {
            return Result.BusinessError("El contenedor ya fue marcado como devuelto vacío.");
        }

        var valoresAnteriores = BuildAuditSnapshot(container);

        var estadoDevueltoVacio = await unitOfWork.EstadosContainer.GetByDescripcionAsync(EstadoDevueltoVacio, ct)
            ?? throw new InvalidOperationException($"El catálogo ESTADO_CONTAINER no tiene una fila '{EstadoDevueltoVacio}'.");

        var ahora = DateTimeOffset.UtcNow;
        container.IdEstado = estadoDevueltoVacio.Id;
        container.EstadoContainer = estadoDevueltoVacio;
        container.UpdateAt = ahora;

        await unitOfWork.Hitos.AddAsync(new Hito
        {
            IdContainer = container.Id,
            Descripcion = "Devolución del contenedor vacío registrada manualmente por el analista.",
            Origen = "USUARIO",
            CreadoPor = userId,
            FechaOcurrencia = ahora,
        }, ct);

        await riskEngineService.RecalcularAsync(container, ct);
        await RegistrarAuditoriaAsync(container, userId, valoresAnteriores, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> ConfirmarArriboRealAsync(int containerId, int userId, DateTimeOffset fechaArriboReal, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);
        if (container is null || container.AsignadoA != userId)
        {
            logger.LogWarning(
                "ConfirmarArriboReal rechazado: container {ContainerId} no existe o no pertenece a userId={UserId}",
                containerId, userId);
            return Result.NotFound($"El contenedor {containerId} no existe o no pertenece al usuario.");
        }

        if (fechaArriboReal > DateTimeOffset.UtcNow)
        {
            return Result.BusinessError("No se puede confirmar un arribo en una fecha futura.");
        }

        var valoresAnteriores = BuildAuditSnapshot(container);

        var ahora = DateTimeOffset.UtcNow;
        container.FechaArriboReal = fechaArriboReal;
        container.DeadlineDemurrage = fechaArriboReal.AddDays(container.DiasLibresDemurrage);
        container.UpdateAt = ahora;

        await unitOfWork.Hitos.AddAsync(new Hito
        {
            IdContainer = container.Id,
            Descripcion = $"Arribo real confirmado manualmente por el analista para el {fechaArriboReal:dd-MM-yyyy}.",
            Origen = "USUARIO",
            CreadoPor = userId,
            FechaOcurrencia = ahora,
        }, ct);

        await riskEngineService.RecalcularAsync(container, ct);
        await RegistrarAuditoriaAsync(container, userId, valoresAnteriores, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> UpdateDiasLibresAsync(int containerId, int userId, int diasLibresDemurrage, int diasLibresDetention, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);
        if (container is null || container.AsignadoA != userId)
        {
            logger.LogWarning(
                "UpdateDiasLibres rechazado: container {ContainerId} no existe o no pertenece a userId={UserId}",
                containerId, userId);
            return Result.NotFound($"El contenedor {containerId} no existe o no pertenece al usuario.");
        }

        if (diasLibresDemurrage < 0 || diasLibresDetention < 0)
        {
            return Result.BusinessError("Los días libres no pueden ser negativos.");
        }

        var valoresAnteriores = BuildAuditSnapshot(container);

        var ahora = DateTimeOffset.UtcNow;
        var demurrageAnterior = container.DiasLibresDemurrage;
        var detentionAnterior = container.DiasLibresDetention;

        container.DiasLibresDemurrage = diasLibresDemurrage;
        container.DiasLibresDetention = diasLibresDetention;

        if (container.FechaArriboReal is not null)
        {
            container.DeadlineDemurrage = container.FechaArriboReal.Value.AddDays(diasLibresDemurrage);
        }

        if (container.FechaRetiroContainer is not null)
        {
            container.DeadlineDetention = container.FechaRetiroContainer.Value.AddDays(diasLibresDetention);
        }

        container.UpdateAt = ahora;

        await unitOfWork.Hitos.AddAsync(new Hito
        {
            IdContainer = container.Id,
            Descripcion = $"Días libres corregidos manualmente: demurrage {demurrageAnterior}→{diasLibresDemurrage}, detention {detentionAnterior}→{diasLibresDetention}.",
            Origen = "USUARIO",
            CreadoPor = userId,
            FechaOcurrencia = ahora,
        }, ct);

        await riskEngineService.RecalcularAsync(container, ct);
        await RegistrarAuditoriaAsync(container, userId, valoresAnteriores, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> UpdateDatosExtraidosAsync(int containerId, int userId, UpdateContainerMetadataRequest request, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);
        if (container is null || container.AsignadoA != userId)
        {
            logger.LogWarning(
                "UpdateDatosExtraidos rechazado: container {ContainerId} no existe o no pertenece a userId={UserId}",
                containerId, userId);
            return Result.NotFound($"El contenedor {containerId} no existe o no pertenece al usuario.");
        }

        Naviera? naviera = null;
        if (request.Naviera is not null)
        {
            naviera = await unitOfWork.Navieras.Query().FirstOrDefaultAsync(n => n.NombreNaviera == request.Naviera, ct);
            if (naviera is null)
            {
                return Result.BusinessError($"La naviera '{request.Naviera}' no existe en el catálogo.");
            }
        }

        Terminal? terminalOrigen = null;
        if (request.TerminalOrigen is not null)
        {
            terminalOrigen = await unitOfWork.Terminales.Query().FirstOrDefaultAsync(t => t.NombreTerminal == request.TerminalOrigen, ct);
            if (terminalOrigen is null)
            {
                return Result.BusinessError($"El terminal de origen '{request.TerminalOrigen}' no existe en el catálogo.");
            }
        }

        Terminal? terminalLlegada = null;
        if (request.TerminalLlegada is not null)
        {
            terminalLlegada = await unitOfWork.Terminales.Query().FirstOrDefaultAsync(t => t.NombreTerminal == request.TerminalLlegada, ct);
            if (terminalLlegada is null)
            {
                return Result.BusinessError($"El terminal de llegada '{request.TerminalLlegada}' no existe en el catálogo.");
            }
        }

        var valoresAnteriores = BuildAuditSnapshot(container);
        var ahora = DateTimeOffset.UtcNow;
        var cambios = new List<string>();

        if (naviera is not null && naviera.Id != container.IdNaviera)
        {
            cambios.Add($"naviera {container.Naviera?.NombreNaviera ?? "—"}→{naviera.NombreNaviera}");
            container.IdNaviera = naviera.Id;
            container.Naviera = naviera;
        }

        if (terminalOrigen is not null && terminalOrigen.Id != container.IdTerminalOrigen)
        {
            cambios.Add($"terminal origen {container.TerminalOrigen?.NombreTerminal ?? "—"}→{terminalOrigen.NombreTerminal}");
            container.IdTerminalOrigen = terminalOrigen.Id;
            container.TerminalOrigen = terminalOrigen;
        }

        if (terminalLlegada is not null && terminalLlegada.Id != container.IdTerminalLlegada)
        {
            cambios.Add($"terminal llegada {container.TerminalLlegada?.NombreTerminal ?? "—"}→{terminalLlegada.NombreTerminal}");
            container.IdTerminalLlegada = terminalLlegada.Id;
            container.TerminalLlegada = terminalLlegada;
        }

        if (request.NumeroBl is not null && request.NumeroBl != container.NumeroBl)
        {
            cambios.Add($"N° BL {container.NumeroBl ?? "—"}→{request.NumeroBl}");
            container.NumeroBl = request.NumeroBl;
        }

        if (request.NombreForwarder is not null && request.NombreForwarder != container.NombreForwarder)
        {
            cambios.Add($"forwarder {container.NombreForwarder ?? "—"}→{request.NombreForwarder}");
            container.NombreForwarder = request.NombreForwarder;
        }

        if (request.Eta is not null && request.Eta != container.FechaLlegadaEstimada)
        {
            cambios.Add($"ETA {container.FechaLlegadaEstimada:dd-MM-yyyy}→{request.Eta:dd-MM-yyyy}");
            container.FechaLlegadaEstimada = request.Eta;
        }

        if (cambios.Count == 0)
        {
            return Result.Success();
        }

        container.UpdateAt = ahora;

        await unitOfWork.Hitos.AddAsync(new Hito
        {
            IdContainer = container.Id,
            Descripcion = $"Datos extraídos corregidos manualmente: {string.Join("; ", cambios)}.",
            Origen = "USUARIO",
            CreadoPor = userId,
            FechaOcurrencia = ahora,
        }, ct);

        await RegistrarAuditoriaAsync(container, userId, valoresAnteriores, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    private async Task RegistrarAuditoriaAsync(Container container, int userId, string valoresAnteriores, CancellationToken ct)
    {
        var accion = await unitOfWork.Acciones.GetByDescripcionAsync(AccionActualizar, ct);
        if (accion is null)
        {
            logger.LogWarning(
                "No se encontró la acción '{Accion}', no se registra auditoría para container_id={ContainerId}",
                AccionActualizar, container.Id);
            return;
        }

        await unitOfWork.LogsAuditoria.AddAsync(new LogAuditoria
        {
            NombreEntidad = nameof(Container),
            IdEntidad = container.Id,
            IdAccion = accion.Id,
            Responsable = userId,
            ValoresAnteriores = valoresAnteriores,
            ValoresNuevos = BuildAuditSnapshot(container),
        }, ct);
    }

    private static string BuildAuditSnapshot(Container container) => JsonSerializer.Serialize(new
    {
        container.FechaArriboReal,
        container.FechaRetiroContainer,
        Estado = container.EstadoContainer?.Descripcion,
        container.DiasLibresDemurrage,
        container.DiasLibresDetention,
        container.DeadlineDemurrage,
        container.DeadlineDetention,
        Naviera = container.Naviera?.NombreNaviera,
        TerminalOrigen = container.TerminalOrigen?.NombreTerminal,
        TerminalLlegada = container.TerminalLlegada?.NombreTerminal,
        container.NumeroBl,
        container.NombreForwarder,
        container.FechaLlegadaEstimada,
    });

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

        if (!string.IsNullOrWhiteSpace(riskLevel))
        {
            var normalizedRiskLevel = riskLevel.Trim().ToUpperInvariant();
            query = query.Where(c => c.Riesgo != null && c.Riesgo.Descripcion == normalizedRiskLevel);
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
