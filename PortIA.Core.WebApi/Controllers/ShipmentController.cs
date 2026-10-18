namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/shipments")]
public class ShipmentsController(IShipmentService shipmentService) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ShipmentCreatedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateShipment(IFormFile file, CancellationToken ct)
    {
        if (IsFileMissing(file))
            return Problem(detail: "Falta el archivo adjunto.", statusCode: StatusCodes.Status400BadRequest, title: "Error de validación");

        var result = await CreateShipmentFromFileAsync(file, ct);
        return result.ToActionResult(this, response => Accepted(response));
    }

    private static bool IsFileMissing(IFormFile? file) => file is null || file.Length == 0;

    private async Task<Result<ShipmentCreatedResponse>> CreateShipmentFromFileAsync(IFormFile file, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        await using var stream = file.OpenReadStream();
        return await shipmentService.CreateShipmentAsync(stream, file.FileName, file.ContentType, file.Length, userId, ct);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShipmentSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShipments(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = "-createdAt",
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();

        var result = await shipmentService.GetShipmentsAsync(
            userId,
            status,
            page,
            pageSize,
            sort,
            ct);

        return result.ToActionResult(this, Ok);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ShipmentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipmentById(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await shipmentService.GetShipmentByIdAsync(id, userId, ct);

        return result.ToActionResult(this, Ok);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
        return userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 1;
    }
}