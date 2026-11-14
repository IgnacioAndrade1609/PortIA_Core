namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class PaisRepository(PortIACoreDbContext dbContext) : IPaisRepository
{
    public Task<Pais?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Paises.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<Pais>> GetAllAsync(CancellationToken ct) =>
        dbContext.Paises.AsNoTracking().OrderBy(p => p.NombrePais).ToListAsync(ct);
}
