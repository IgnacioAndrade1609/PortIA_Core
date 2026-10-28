namespace PortIA.Core.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Users.DTOs;
using PortIA.Core.Services.Component.Users.Interfaces;
using PortIA.Core.WebApi.Extensions;
using PortIA.Core.Services.Common.DTOs;

[ApiController]
[Route("api/v1/users")]
//[Authorize(Roles = "Admin")] //quitar este comentario para activar la autorizacion con jwt
public class UsersController(IUsuarioService usuarioService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] UserQueryFilter filter, CancellationToken ct)
    {
        var result = await usuarioService.GetPagedAsync(filter, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await usuarioService.GetByIdAsync(id, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUsuarioRequest request, CancellationToken ct)
    {
        var result = await usuarioService.CreateAsync(request, ct);
        return result.ToActionResult(this, response => Created($"/api/v1/users/{response.Id}", response));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateUsuarioRequest request, CancellationToken ct)
    {
        var result = await usuarioService.UpdateAsync(id, request, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateUsuarioStatusRequest request, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        var result = await usuarioService.UpdateStatusAsync(id, request, currentUserId, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPut("{id:int}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRole(int id, UpdateUsuarioRoleRequest request, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        var result = await usuarioService.UpdateRoleAsync(id, request, currentUserId, ct);
        return result.ToActionResult(this, Ok);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var userId) ? userId : 0;
    }
}