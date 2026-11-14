namespace PortIA.Core.Services.Component.Teams.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Teams.DTOs;

public interface ITeamDashboardService
{
    Task<Result<TeamDashboardResponse>> GetDashboardAsync(
        int jefeId, int callerUserId, string callerRole, TeamDashboardFilter filter, CancellationToken ct);
}
