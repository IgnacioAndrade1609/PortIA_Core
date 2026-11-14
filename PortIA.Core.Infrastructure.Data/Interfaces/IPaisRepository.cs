namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IPaisRepository
{
    Task<Pais?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<Pais>> GetAllAsync(CancellationToken ct);
}
