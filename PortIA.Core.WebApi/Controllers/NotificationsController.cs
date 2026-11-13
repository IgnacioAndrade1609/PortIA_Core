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

    [HttpPost("draft")]
    [ProducesResponseType(typeof(NotificationDraftResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDraft(CreateDraftRequest request, CancellationToken ct)
    {
        var result = await notificationService.CreateDraftAsync(request, ct);
        return result.ToActionResult(this, response => Created($"/api/v1/notifications/{response.Id}", response));
    }


    [HttpPut("draft/{id:int}")]
    [ProducesResponseType(typeof(NotificationDraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraft(int id, UpdateDraftRequest request, CancellationToken ct)
    {
        var result = await notificationService.UpdateDraftAsync(id, request, ct);
        return result.ToActionResult(this, Ok);
    }


    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(NotificationDraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(int id, ApproveNotificationRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await notificationService.ApproveAsync(id, userId, request, ct);
        return result.ToActionResult(this, Ok);
    }


    [HttpGet("alertas/mias")]
    [ProducesResponseType(typeof(List<AlertaIdResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMisAlertas(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await notificationService.GetAlertasByUsuarioAsync(userId, ct);
        return result.ToActionResult(this, Ok);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
        return userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }
}