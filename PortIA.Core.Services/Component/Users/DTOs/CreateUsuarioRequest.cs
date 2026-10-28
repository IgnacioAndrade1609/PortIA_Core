namespace PortIA.Core.Services.Component.Users.DTOs;

public record CreateUsuarioRequest(
    string NombreUsuario,
    string Email,
    string Password,
    int RolId
);
