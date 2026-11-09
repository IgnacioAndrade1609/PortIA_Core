namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class ClimaRepository(PortIACoreDbContext dbContext) : IClimaRepository
{
    public Task<Clima?> GetByDescripcionAsync(string descripcion, CancellationToken ct) =>
        dbContext.Climas.FirstOrDefaultAsync(c => c.Descripcion == descripcion, ct);
}
