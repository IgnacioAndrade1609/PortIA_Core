namespace PortIA.Core.Infrastructure.Data.Entities;

public class Rol
{
    public int Id { get; set; }
    public string NombreRol { get; set; } = null!;
    public bool Vigente { get; set; } = true;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
