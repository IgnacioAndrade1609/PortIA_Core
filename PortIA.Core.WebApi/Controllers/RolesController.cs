namespace PortIA.Core.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Users.DTOs;
using PortIA.Core.Services.Component.Users.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Roles = "Admin")]
public class RolesController(IUsuarioService usuarioService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await usuarioService.GetRolesAsync(ct);
        return result.ToActionResult(this, Ok);
    }
}