namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IAlertaRepository
{
    Task AddAsync(Alerta entity, CancellationToken ct);
    Task<Alerta?> GetByIdAsync(int id, CancellationToken ct);
    IQueryable<Alerta> Query();
}
