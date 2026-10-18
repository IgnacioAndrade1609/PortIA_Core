namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IHitoRepository
{
    Task AddAsync(Hito entity, CancellationToken ct);
}
