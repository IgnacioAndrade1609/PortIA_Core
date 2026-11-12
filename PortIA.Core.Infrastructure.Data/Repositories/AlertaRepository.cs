namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class AlertaRepository(PortIACoreDbContext dbContext) : IAlertaRepository
{
    public Task AddAsync(Alerta entity, CancellationToken ct)
    {
        dbContext.Alertas.Add(entity);
        return Task.CompletedTask;
    }

    public Task<Alerta?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Alertas
            .Include(a => a.Container)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public IQueryable<Alerta> Query() =>
        dbContext.Alertas.AsNoTracking()
            .Include(a => a.Container)
            .Include(a => a.TipoAlerta)
            .Include(a => a.Riesgo);
}
