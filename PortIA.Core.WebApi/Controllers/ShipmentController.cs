namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Interfaces;

[ApiController]
[Route("api/v1/shipments")]
public class ShipmentsController(IShipmentService shipmentService) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ShipmentCreatedResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateShipment(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { title = "Error de Validación", detail = "Falta el archivo adjunto.", status = 400 });
        }

        try
        {
            var userId = GetCurrentUserId();

            await using var stream = file.OpenReadStream();

            var result = await shipmentService.CreateShipmentAsync(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                userId,
                ct);

            return Accepted(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { title = "Documento Duplicado", detail = ex.Message, status = 409 });
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or NotSupportedException)
        {
            return BadRequest(new { title = "Error de Validación", detail = ex.Message, status = 400 });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShipmentSummaryDto>), StatusCodes.Status200OK)]
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

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ShipmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipmentById(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await shipmentService.GetShipmentByIdAsync(id, userId, ct);

        if (result is null)
        {
            return NotFound(new { title = "No encontrado", detail = "La carga no existe o no pertenece al usuario.", status = 404 });
        }

        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var claim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
        if (claim != null && int.TryParse(claim.Value, out var id))
        {
            return id;
        }
        return 1;
    }
}