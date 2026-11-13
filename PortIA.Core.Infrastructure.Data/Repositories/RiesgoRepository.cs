namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class RiesgoRepository(PortIACoreDbContext dbContext) : IRiesgoRepository
{
    public Task<Riesgo?> GetByDescripcionAsync(string descripcion, CancellationToken ct) =>
        dbContext.Riesgos.FirstOrDefaultAsync(r => r.Descripcion == descripcion, ct);
}
