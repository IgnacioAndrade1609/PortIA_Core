namespace PortIA.Core.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Navieras.DTOs;
using PortIA.Core.Services.Component.Navieras.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/navieras")]
[Authorize]
public class NavierasController(INavieraService navieraService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NavieraDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await navieraService.GetAllAsync(ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NavieraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await navieraService.GetByIdAsync(id, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(NavieraDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateNavieraRequest request, CancellationToken ct)
    {
        var result = await navieraService.CreateAsync(request, ct);
        return result.ToActionResult(this, response => Created($"/api/v1/navieras/{response.Id}", response));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(NavieraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateNavieraRequest request, CancellationToken ct)
    {
        var result = await navieraService.UpdateAsync(id, request, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPatch("{id:int}/vigencia")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(NavieraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVigencia(int id, UpdateNavieraVigenciaRequest request, CancellationToken ct)
    {
        var result = await navieraService.UpdateVigenciaAsync(id, request, ct);
        return result.ToActionResult(this, Ok);
    }
}
