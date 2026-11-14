namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class LogAuditoriaRepository(PortIACoreDbContext dbContext) : ILogAuditoriaRepository
{
    public Task AddAsync(LogAuditoria entity, CancellationToken ct)
    {
        dbContext.LogsAuditoria.Add(entity);
        return Task.CompletedTask;
    }

    public IQueryable<LogAuditoria> Query() =>
        dbContext.LogsAuditoria
            .Include(x => x.Accion)
            .Include(x => x.ResponsableUsuario);
}
