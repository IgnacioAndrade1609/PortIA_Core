namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Component.Audit.DTOs;
using PortIA.Core.Services.Component.Audit.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/audit")]
[Authorize]
public class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] AuditQueryFilter filter, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await auditService.GetPagedAsync(filter, userId, userRole, ct);

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
