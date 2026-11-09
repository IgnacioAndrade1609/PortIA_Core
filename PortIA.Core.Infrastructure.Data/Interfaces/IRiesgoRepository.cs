namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IRiesgoRepository
{
    Task<Riesgo?> GetByDescripcionAsync(string descripcion, CancellationToken ct);
}
