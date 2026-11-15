namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Admin.DTOs;
using PortIA.Core.Services.Component.Admin.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
public class AdminController(ISystemHealthService systemHealthService) : ControllerBase
{
    [HttpGet("system-health")]
    [ProducesResponseType(typeof(SystemHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSystemHealth(CancellationToken ct)
    {
        var role = (User?.FindFirst(ClaimTypes.Role) ?? User?.FindFirst("role"))?.Value;
        if (role is null) return Unauthorized();
        if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return Forbid();

        var result = await systemHealthService.GetAsync(ct);
        return result.ToActionResult(this, Ok);
    }
}
