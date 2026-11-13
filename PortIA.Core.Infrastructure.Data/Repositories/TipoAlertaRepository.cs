namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class TipoAlertaRepository(PortIACoreDbContext dbContext) : ITipoAlertaRepository
{
    public Task<TipoAlerta?> GetByDescripcionAsync(string descripcion, CancellationToken ct) =>
        dbContext.TiposAlerta.FirstOrDefaultAsync(t => t.Descripcion == descripcion, ct);
}
