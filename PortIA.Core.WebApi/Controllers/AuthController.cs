namespace PortIA.Core.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using PortIA.Core.Services.Component.Auth.DTOs;
using PortIA.Core.Services.Component.Auth.Interfaces;
using PortIA.Core.WebApi.Extensions;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result.ToActionResult(this, Ok);
    }
}
