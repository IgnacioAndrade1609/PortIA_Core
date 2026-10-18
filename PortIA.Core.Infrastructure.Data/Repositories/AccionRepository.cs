namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class AccionRepository(PortIACoreDbContext dbContext) : IAccionRepository
{
    public Task<Accion?> GetByDescripcionAsync(string descripcion, CancellationToken ct) =>
        dbContext.Acciones.FirstOrDefaultAsync(a => a.Descripcion == descripcion, ct);
}
