namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Notifications.DTOs;
using PortIA.Core.Services.Component.Notifications.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet("alertas/mias")]
    [ProducesResponseType(typeof(List<AlertaPendienteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMisAlertas(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var result = await notificationService.GetAlertasPendientesByUsuarioAsync(userId, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NotificationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotificationDetail(
        int id,
        [FromQuery] int canalId = 1,
        CancellationToken ct = default)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var result = await notificationService.GetNotificationDetailAsync(id, userId, canalId, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPost("{id:int}/send")]
    [ProducesResponseType(typeof(SendNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send(
        int id,
        [FromBody] SendNotificationRequest request,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var result = await notificationService.SendAsync(id, userId, request, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(SendNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] RejectNotificationRequest request,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var result = await notificationService.RejectAsync(id, userId, request, ct);
        return result.ToActionResult(this, Ok);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var claim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
        userId = 0;
        return claim is not null && int.TryParse(claim.Value, out userId) && userId > 0;
    }
}
