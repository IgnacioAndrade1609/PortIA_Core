namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class LogNotificacionRepository(PortIACoreDbContext dbContext) : ILogNotificacionRepository
{
    public Task AddAsync(LogNotificacion entity, CancellationToken ct)
    {
        dbContext.LogsNotificacion.Add(entity);
        return Task.CompletedTask;
    }

    public Task<LogNotificacion?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.LogsNotificacion
            .Include(l => l.Alerta)
                .ThenInclude(a => a.Container)
            .Include(l => l.Alerta)
                .ThenInclude(a => a.TipoAlerta)
            .Include(l => l.Alerta)
                .ThenInclude(a => a.Riesgo)
            .Include(l => l.Canal)
            .Include(l => l.AprobadoPorUsuario)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public IQueryable<LogNotificacion> Query() =>
        dbContext.LogsNotificacion
            .AsNoTracking()
            .Include(l => l.Alerta)
                .ThenInclude(a => a.Container)
            .Include(l => l.Alerta)
                .ThenInclude(a => a.TipoAlerta)
            .Include(l => l.Alerta)
                .ThenInclude(a => a.Riesgo)
            .Include(l => l.Canal);
}