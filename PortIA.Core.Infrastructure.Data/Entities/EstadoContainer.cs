namespace PortIA.Core.Infrastructure.Data.Entities;

public class EstadoContainer
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Vigente { get; set; } = true;
    
    // Navegación inversa
    public ICollection<Container> Containers { get; set; } = [];
}