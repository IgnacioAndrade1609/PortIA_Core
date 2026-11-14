namespace PortIA.Core.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Terminales.DTOs;
using PortIA.Core.Services.Component.Terminales.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/paises")]
[Authorize]
public class PaisesController(ITerminalService terminalService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PaisDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await terminalService.GetPaisesAsync(ct);
        return result.ToActionResult(this, Ok);
    }
}
