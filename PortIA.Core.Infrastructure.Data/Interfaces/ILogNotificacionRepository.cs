namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface ILogNotificacionRepository
{
    Task AddAsync(LogNotificacion entity, CancellationToken ct);
    Task<LogNotificacion?> GetByIdAsync(int id, CancellationToken ct);
    IQueryable<LogNotificacion> Query();
}