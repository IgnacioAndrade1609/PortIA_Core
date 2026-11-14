namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class NavieraRepository(PortIACoreDbContext dbContext) : INavieraRepository
{
    public Task<Naviera?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Navieras.FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task<List<Naviera>> GetAllAsync(CancellationToken ct) =>
        dbContext.Navieras.AsNoTracking().OrderBy(n => n.NombreNaviera).ToListAsync(ct);

    public Task<bool> ExistsByNombreAsync(string nombre, int? excludeId, CancellationToken ct)
    {
        var normalizado = nombre.ToLower();
        return dbContext.Navieras.AnyAsync(
            n => n.NombreNaviera.ToLower() == normalizado && (excludeId == null || n.Id != excludeId), ct);
    }

    public Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct)
    {
        var normalizado = codigo.ToLower();
        return dbContext.Navieras.AnyAsync(
            n => n.CodigoNaviera != null && n.CodigoNaviera.ToLower() == normalizado && (excludeId == null || n.Id != excludeId), ct);
    }

    public async Task AddAsync(Naviera naviera, CancellationToken ct) =>
        await dbContext.Navieras.AddAsync(naviera, ct);

    public IQueryable<Naviera> Query() =>
        dbContext.Navieras.AsNoTracking().Where(n => n.Vigente);
}
