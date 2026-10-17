namespace PortIA.Core.Infrastructure.Data.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public int RolId { get; set; }
    public DateTimeOffset Creacion { get; set; } = DateTimeOffset.UtcNow;
    public bool Vigente { get; set; } = true;

    public Rol Rol { get; set; } = null!;
}
