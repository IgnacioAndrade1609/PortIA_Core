namespace PortIA.Core.Infrastructure.Data.Entities;

public class Pais
{
    public int Id { get; set; }
    public string NombrePais { get; set; } = null!;

    public ICollection<Terminal> Terminales { get; set; } = new List<Terminal>();
}
