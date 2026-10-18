namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface ILogAuditoriaRepository
{
    Task AddAsync(LogAuditoria entity, CancellationToken ct);
}
