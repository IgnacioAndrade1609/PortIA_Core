namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IAccionRepository
{
    Task<Accion?> GetByDescripcionAsync(string descripcion, CancellationToken ct);
}
