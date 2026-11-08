namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public class DocumentsController(IDocumentReviewService documentReviewService) : ControllerBase
{
    [HttpGet("pendientes-revision")]
    [ProducesResponseType(typeof(List<PendingDocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendientesRevision(CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await documentReviewService.GetPendientesAsync(userId, ct);

        return result.ToActionResult(this, Ok);
    }

    [HttpPut("{id:int}/validacion")]
    [ProducesResponseType(typeof(ValidateDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Validar(int id, ValidateDocumentRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await documentReviewService.ValidarAsync(id, userId, request, ct);

        return result.ToActionResult(this, Ok);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
        return userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 1;
    }
}
