namespace PortIA.Core.Services.Component.Auth.DTOs;

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Rol { get; set; } = null!;
}
