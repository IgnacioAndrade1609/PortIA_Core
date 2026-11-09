namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class NavieraRepository(PortIACoreDbContext dbContext) : INavieraRepository
{
    public Task<Naviera?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Navieras.FirstOrDefaultAsync(n => n.Id == id, ct);

    public IQueryable<Naviera> Query() =>
        dbContext.Navieras.AsNoTracking().Where(n => n.Vigente);
}
