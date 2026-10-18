namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class ContainerRepository(PortIACoreDbContext dbContext) : IContainerRepository
{
    public Task AddAsync(Container entity, CancellationToken ct)
    {
        dbContext.Containers.Add(entity);
        return Task.CompletedTask;
    }

    public Task<Container?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Containers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public IQueryable<Container> Query() =>
        dbContext.Containers.AsNoTracking().Where(c => c.Vigente);
}
