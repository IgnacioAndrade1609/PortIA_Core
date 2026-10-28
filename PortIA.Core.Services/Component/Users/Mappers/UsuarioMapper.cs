namespace PortIA.Core.Services.Component.Users.Mappers;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Component.Users.DTOs;

public static class UsuarioMapper
{
    public static Usuario ToEntity(this CreateUsuarioRequest request, string passwordHash) => new()
    {
        NombreUsuario = request.NombreUsuario,
        Email = request.Email,
        PasswordHash = passwordHash,
        RolId = request.RolId,
        Vigente = true
    };

    public static UsuarioResponse ToResponse(this Usuario usuario) => new(
        usuario.Id,
        usuario.NombreUsuario,
        usuario.Email,
        usuario.Rol.NombreRol,
        usuario.Vigente,
        usuario.Creacion
    );
}
