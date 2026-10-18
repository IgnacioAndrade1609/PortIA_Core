namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IClimaRepository
{
    Task<Clima?> GetByDescripcionAsync(string descripcion, CancellationToken ct);
}
