namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IContainerRepository
{
    Task AddAsync(Container entity, CancellationToken ct);
    Task<Container?> GetByIdAsync(int id, CancellationToken ct);
    IQueryable<Container> Query();
}
