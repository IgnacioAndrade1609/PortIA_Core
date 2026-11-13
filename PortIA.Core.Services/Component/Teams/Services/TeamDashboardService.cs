namespace PortIA.Core.Services.Component.Teams.Services;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Teams.DTOs;
using PortIA.Core.Services.Component.Teams.Interfaces;

public class TeamDashboardService(
    IUnitOfWork unitOfWork,
    IValidator<TeamDashboardFilter> validator) : ITeamDashboardService
{
    private static readonly string[] RiskLevelsOrder = ["OK", "WARNING", "CRITICAL", "OVERDUE"];

    public async Task<Result<TeamDashboardResponse>> GetDashboardAsync(
        int jefeId, int callerUserId, string callerRole, TeamDashboardFilter filter, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(filter, ct);
        if (!validation.IsValid)
            return Result<TeamDashboardResponse>.ValidationError(validation.ToDictionary());

        var normalizedRole = callerRole.ToUpperInvariant();
        int effectiveJefeId;
        if (normalizedRole is "JEFE" or "BOSS")
        {
            effectiveJefeId = callerUserId;
        }
        else if (normalizedRole == "ADMIN")
        {
            effectiveJefeId = jefeId;
        }
        else
        {
            return Result<TeamDashboardResponse>.Unauthorized("No tiene permisos para ver este dashboard.");
        }

        var jefe = await unitOfWork.Usuarios.GetByIdWithRoleAsync(effectiveJefeId, ct);
        if (jefe is null)
            return Result<TeamDashboardResponse>.NotFound($"No se encontró el jefe con ID {effectiveJefeId}.");

        if (!jefe.Rol.NombreRol.Equals("Jefe", StringComparison.OrdinalIgnoreCase))
            return Result<TeamDashboardResponse>.BusinessError("El usuario indicado no tiene rol Jefe.");

        var toDate = filter.ToDate ?? DateTimeOffset.UtcNow;
        var fromDate = filter.FromDate ?? toDate.AddDays(-30);

        var containersByRisk = await GetContainersByRiskAsync(effectiveJefeId, ct);
        var (alertsByAnalyst, avgReaction) = await GetAlertMetricsAsync(effectiveJefeId, fromDate, toDate, ct);
        var documentsByPeriod = await GetDocumentsByPeriodAsync(effectiveJefeId, fromDate, toDate, ct);

        var response = new TeamDashboardResponse(
            fromDate, toDate, containersByRisk, alertsByAnalyst, avgReaction, documentsByPeriod);

        return Result<TeamDashboardResponse>.Success(response);
    }

    private async Task<List<ContainersByRiskDto>> GetContainersByRiskAsync(int jefeId, CancellationToken ct)
    {
        var counts = await unitOfWork.Containers.Query()
            .Where(c => c.Vigente && c.Asignado != null && c.Asignado.JefeId == jefeId)
            .GroupBy(c => c.Riesgo != null ? c.Riesgo.Descripcion : "OK")
            .Select(g => new { RiskLevel = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countsByLevel = counts.ToDictionary(x => x.RiskLevel, x => x.Count);

        return RiskLevelsOrder
            .Select(level => new ContainersByRiskDto(level, countsByLevel.GetValueOrDefault(level, 0)))
            .ToList();
    }

    private async Task<(List<AlertsByAnalystDto> AlertsByAnalyst, double? AvgReactionMinutes)> GetAlertMetricsAsync(
        int jefeId, DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken ct)
    {
        var (analistas, _) = await unitOfWork.Usuarios.GetPagedAsync(
            search: null, roleId: null, jefeId: jefeId, active: null, page: 1, pageSize: 1000, ct);

        var alertasEnPeriodo = await unitOfWork.Alertas.Query()
            .Where(a => a.Container.Asignado != null && a.Container.Asignado.JefeId == jefeId
                        && a.CreateAt >= fromDate && a.CreateAt <= toDate)
            .Select(a => new
            {
                AnalystId = a.Container.AsignadoA!.Value,
                a.Leida,
                a.CreateAt,
                a.FechaLectura,
                RiskLevel = a.Riesgo.Descripcion
            })
            .ToListAsync(ct);

        var statsByAnalyst = alertasEnPeriodo
            .GroupBy(a => a.AnalystId)
            .ToDictionary(g => g.Key, g => (Generated: g.Count(), Managed: g.Count(x => x.Leida)));

        var alertsByAnalyst = analistas
            .Select(an =>
            {
                var stats = statsByAnalyst.GetValueOrDefault(an.Id, (Generated: 0, Managed: 0));
                return new AlertsByAnalystDto(an.Id, an.NombreUsuario, stats.Generated, stats.Managed);
            })
            .OrderBy(a => a.AnalystName)
            .ToList();

        var tiemposReaccionCritical = alertasEnPeriodo
            .Where(a => a.RiskLevel == "CRITICAL" && a.Leida && a.FechaLectura.HasValue)
            .Select(a => (a.FechaLectura!.Value - a.CreateAt).TotalMinutes)
            .ToList();

        double? avgReaction = tiemposReaccionCritical.Count > 0 ? tiemposReaccionCritical.Average() : null;

        return (alertsByAnalyst, avgReaction);
    }

    private async Task<List<DocumentsByDayDto>> GetDocumentsByPeriodAsync(
        int jefeId, DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken ct)
    {
        var raw = await unitOfWork.CargasDocumentos.Query()
            .Where(cd => cd.Vigente && cd.Usuario.JefeId == jefeId
                         && cd.CreacionFecha >= fromDate && cd.CreacionFecha <= toDate)
            .Select(cd => cd.CreacionFecha)
            .ToListAsync(ct);

        var countsByDay = raw
            .GroupBy(f => DateOnly.FromDateTime(f.UtcDateTime.Date))
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<DocumentsByDayDto>();
        for (var day = DateOnly.FromDateTime(fromDate.UtcDateTime.Date);
             day <= DateOnly.FromDateTime(toDate.UtcDateTime.Date);
             day = day.AddDays(1))
        {
            result.Add(new DocumentsByDayDto(day, countsByDay.GetValueOrDefault(day, 0)));
        }

        return result;
    }
}
