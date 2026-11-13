namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Teams.DTOs;
using PortIA.Core.Services.Component.Teams.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/teams")]
[Authorize]
public class TeamsController(
    ITeamDashboardService teamDashboardService,
    ITeamDocumentsService teamDocumentsService) : ControllerBase
{
    [HttpGet("{jefeId:int}/dashboard")]
    [ProducesResponseType(typeof(TeamDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetDashboard(int jefeId, [FromQuery] TeamDashboardFilter filter, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await teamDashboardService.GetDashboardAsync(jefeId, userId, userRole, filter, ct);

        return result.ToActionResult(this, Ok);
    }

    [HttpGet("{jefeId:int}/documents")]
    [ProducesResponseType(typeof(List<AnalystDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetDocuments(int jefeId, [FromQuery] TeamDocumentsFilter filter, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await teamDocumentsService.GetDocumentsAsync(jefeId, userId, userRole, filter, ct);

        return result.ToActionResult(this, Ok);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
        return userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 1;
    }

    private string GetCurrentUserRole()
    {
        var roleClaim = User?.FindFirst(ClaimTypes.Role) ?? User?.FindFirst("role");
        return roleClaim?.Value ?? "Admin";
    }
}
