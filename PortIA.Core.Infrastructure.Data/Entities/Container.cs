namespace PortIA.Core.Infrastructure.Data.Entities;

public class Container
{
    public int Id { get; set; }
    public int IdCargaDocumentos { get; set; }
    public string NumeroContainer { get; set; } = null!;
    public int? IdNaviera { get; set; }
    public string? PuertoDestino { get; set; }
    public string? NumeroBl { get; set; }
    public string? NombreForwarder { get; set; }

    public int? IdTerminalOrigen { get; set; }
    public int? IdTerminalLlegada { get; set; }

    public DateTimeOffset? FechaLlegadaEstimada { get; set; }
    public int DiasLibresDemurrage { get; set; }
    public int DiasLibresDetention { get; set; }

    public DateTimeOffset? FechaArriboReal { get; set; }
    public DateTimeOffset? FechaRetiroContainer { get; set; }
    public string? EstadoPuerto { get; set; }
    public int? IdClima { get; set; }

    public DateTimeOffset? DeadlineDemurrage { get; set; }
    public DateTimeOffset? DeadlineDetention { get; set; }
    public int? IdRiesgo { get; set; }

    public int? AsignadoA { get; set; }

    public DateTimeOffset CreateAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdateAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Vigente { get; set; } = true;
    public int? IdEstado { get; set; }
    public CargaDocumentos CargaDocumentos { get; set; } = null!;
    public Naviera? Naviera { get; set; }
    public Terminal? TerminalOrigen { get; set; }
    public Terminal? TerminalLlegada { get; set; }
    public Clima? Clima { get; set; }
    public Riesgo? Riesgo { get; set; }
    public Usuario? Asignado { get; set; }

    public ICollection<Hito> Hitos { get; set; } = new List<Hito>();
    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
    public EstadoContainer? EstadoContainer { get; set; }
}
