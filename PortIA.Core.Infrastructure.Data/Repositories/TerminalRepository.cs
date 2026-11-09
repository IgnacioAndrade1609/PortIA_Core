namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class TerminalRepository(PortIACoreDbContext dbContext) : ITerminalRepository
{
    public Task<Terminal?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Terminales.FirstOrDefaultAsync(t => t.Id == id, ct);

    public IQueryable<Terminal> Query() =>
        dbContext.Terminales.AsNoTracking().Where(t => t.Vigente);
}
