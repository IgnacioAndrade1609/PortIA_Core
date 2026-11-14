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
    public async Task<IActionResult> GetMisAlertas(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await notificationService.GetAlertasPendientesByUsuarioAsync(userId, ct);
        return result.ToActionResult(this, Ok);
    }


    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NotificationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotificationDetail(
        int id,
        [FromQuery] int canalId = 1,
        CancellationToken ct = default)
    {
        var result = await notificationService.GetNotificationDetailAsync(id, canalId, ct);
        return result.ToActionResult(this, Ok);
    }


    [HttpPost("{id:int}/send")]
    [ProducesResponseType(typeof(SendNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send(
        int id,
        [FromBody] SendNotificationRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await notificationService.SendAsync(id, userId, request, ct);
        return result.ToActionResult(this, Ok);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
        return userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }
}