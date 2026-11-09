namespace PortIA.Core.Services.Component.Auth.Mappers;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Common.Security;
using PortIA.Core.Services.Component.Auth.DTOs;

public static class LoginMapper
{
    public static LoginResponse ToLoginResponse(this Usuario usuario, JwtTokenResult token) => new()
    {
        Token = token.Token,
        ExpiresAt = token.ExpiresAt,
        UsuarioId = usuario.Id,
        NombreUsuario = usuario.NombreUsuario,
        Email = usuario.Email,
        Rol = usuario.Rol.NombreRol
    };
}
