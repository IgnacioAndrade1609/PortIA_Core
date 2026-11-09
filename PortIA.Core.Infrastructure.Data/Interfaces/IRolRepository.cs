namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IRolRepository
{
    Task<Rol?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<Rol>> GetAllAsync(CancellationToken ct);
}