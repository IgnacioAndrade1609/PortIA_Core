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

    public static UserDto ToDto(this Usuario usuario) => new()
    {
        Id = usuario.Id,
        Name = usuario.NombreUsuario,
        Email = usuario.Email,
        Role = new RoleSummaryDto
        {
            Id = usuario.Rol.Id,
            Name = usuario.Rol.NombreRol
        },
        Active = usuario.Vigente,
        CreatedAt = usuario.Creacion
    };

    public static RoleDto ToDto(this Rol rol) => new()
    {
        Id = rol.Id,
        Name = rol.NombreRol,
        Active = rol.Vigente
    };
}