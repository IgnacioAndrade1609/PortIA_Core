using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Mensajeria.DTOs;
using PortIA.Core.Services.Component.Mensajeria.Interfaces;

namespace PortIA.Core.WebApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestMessagingController(IMensajeriaService mensajeriaService) : ControllerBase
{
    [HttpPost("publish-shipment-created")]
    public async Task<IActionResult> PublishShipmentCreated(PublishShipmentCreatedRequest request, CancellationToken ct)
    {
        await mensajeriaService.PublishShipmentCreatedAsync(request, ct);
        return Ok(new { mensaje = "ShipmentCreated publicado" });
    }
}
