namespace PortIA.Core.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Terminales.DTOs;
using PortIA.Core.Services.Component.Terminales.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/terminales")]
[Authorize]
public class TerminalesController(ITerminalService terminalService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TerminalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await terminalService.GetAllAsync(ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await terminalService.GetByIdAsync(id, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TerminalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateTerminalRequest request, CancellationToken ct)
    {
        var result = await terminalService.CreateAsync(request, ct);
        return result.ToActionResult(this, response => Created($"/api/v1/terminales/{response.Id}", response));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateTerminalRequest request, CancellationToken ct)
    {
        var result = await terminalService.UpdateAsync(id, request, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPatch("{id:int}/vigencia")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVigencia(int id, UpdateTerminalVigenciaRequest request, CancellationToken ct)
    {
        var result = await terminalService.UpdateVigenciaAsync(id, request, ct);
        return result.ToActionResult(this, Ok);
    }
}
