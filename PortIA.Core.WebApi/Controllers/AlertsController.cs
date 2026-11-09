namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertsController(IAlertService alertService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAlerts([FromQuery] bool? leida, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await alertService.GetAlertsAsync(userId, userRole, leida, ct);

        return result.ToActionResult(this, Ok);
    }

    [HttpPut("{id:int}/leida")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await alertService.MarkAsReadAsync(id, userId, ct);

        return result.ToActionResult(this, () => NoContent());
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
