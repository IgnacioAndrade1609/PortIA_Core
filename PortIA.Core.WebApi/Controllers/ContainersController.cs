namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Containers.DTOs;
using PortIA.Core.Services.Component.Containers.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/containers")]
public class ContainersController(IContainerService containerService) : ControllerBase
{
    [HttpPost("{id:int}/inspect")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Inspect(int id, CancellationToken ct)
    {
        var result = await containerService.TriggerInspectionAsync(id, ct);
        return result.ToActionResult(this, Accepted);
    }

    [HttpGet("{id:int}/hitos")]
    [ProducesResponseType(typeof(IReadOnlyList<HitoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHitos(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await containerService.GetHitosAsync(id, userId, ct);

        return result.ToActionResult(this, Ok);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
        return userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 1;
    }
}
