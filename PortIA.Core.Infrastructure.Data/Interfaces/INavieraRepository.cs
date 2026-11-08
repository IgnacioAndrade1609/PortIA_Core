namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface INavieraRepository
{
    Task<Naviera?> GetByIdAsync(int id, CancellationToken ct);
    IQueryable<Naviera> Query();
}
