namespace PortIA.Core.Infrastructure.Data.Repositories;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class LogAuditoriaRepository(PortIACoreDbContext dbContext) : ILogAuditoriaRepository
{
    public Task AddAsync(LogAuditoria entity, CancellationToken ct)
    {
        dbContext.LogsAuditoria.Add(entity);
        return Task.CompletedTask;
    }
}
