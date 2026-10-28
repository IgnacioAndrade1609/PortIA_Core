namespace PortIA.Core.Services.Component.Users.DTOs;

public record UsuarioResponse(
    int Id,
    string NombreUsuario,
    string Email,
    string Rol,
    bool Vigente,
    DateTimeOffset Creacion
);
