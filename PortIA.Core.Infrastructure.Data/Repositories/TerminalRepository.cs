namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class TerminalRepository(PortIACoreDbContext dbContext) : ITerminalRepository
{
    public Task<Terminal?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Terminales.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Terminal?> GetByIdWithPaisAsync(int id, CancellationToken ct) =>
        dbContext.Terminales.Include(t => t.Pais).FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<Terminal>> GetAllWithPaisAsync(CancellationToken ct) =>
        dbContext.Terminales.AsNoTracking().Include(t => t.Pais).OrderBy(t => t.NombreTerminal).ToListAsync(ct);

    public Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct)
    {
        var normalizado = codigo.ToLower();
        return dbContext.Terminales.AnyAsync(
            t => t.CodigoTerminal.ToLower() == normalizado && (excludeId == null || t.Id != excludeId), ct);
    }

    public async Task AddAsync(Terminal terminal, CancellationToken ct) =>
        await dbContext.Terminales.AddAsync(terminal, ct);

    public IQueryable<Terminal> Query() =>
        dbContext.Terminales.AsNoTracking().Where(t => t.Vigente);
}
