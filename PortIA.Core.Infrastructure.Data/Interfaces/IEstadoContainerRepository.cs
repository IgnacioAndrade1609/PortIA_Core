namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IEstadoContainerRepository
{
    Task<EstadoContainer?> GetByDescripcionAsync(string descripcion, CancellationToken ct);
}
