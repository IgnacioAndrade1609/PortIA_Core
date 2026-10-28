namespace PortIA.Core.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Users.DTOs;
using PortIA.Core.Services.Component.Users.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController(IUsuarioService usuarioService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUsuarioRequest request, CancellationToken ct)
    {
        var result = await usuarioService.CreateAsync(request, ct);
        return result.ToActionResult(this, response => Created($"/api/v1/users/{response.Id}", response));
    }
}
