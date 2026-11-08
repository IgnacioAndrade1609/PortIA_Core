namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface ITipoAlertaRepository
{
    Task<TipoAlerta?> GetByDescripcionAsync(string descripcion, CancellationToken ct);
}
