namespace PortIA.Core.Infrastructure.Data.Repositories;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class HitoRepository(PortIACoreDbContext dbContext) : IHitoRepository
{
    public Task AddAsync(Hito entity, CancellationToken ct)
    {
        dbContext.Hitos.Add(entity);
        return Task.CompletedTask;
    }
}
