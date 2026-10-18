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
    [HttpGet]
    [ProducesResponseType(typeof(List<ContainerSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContainers(
        [FromQuery] string? search,
        [FromQuery] string? riskLevel,
        [FromQuery] int? shippingLineId,
        [FromQuery] int? originTerminalId,
        [FromQuery] int? destinationTerminalId,
        [FromQuery] int? assignedTo,
        [FromQuery] bool? managed,
        [FromQuery] bool? needsReview,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await containerService.GetContainersAsync(
            userId,
            userRole,
            search,
            riskLevel,
            shippingLineId,
            originTerminalId,
            destinationTerminalId,
            assignedTo,
            managed,
            needsReview,
            fromDate,
            toDate,
            ct);

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