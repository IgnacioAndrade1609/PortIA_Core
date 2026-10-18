namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface ITerminalRepository
{
    Task<Terminal?> GetByIdAsync(int id, CancellationToken ct);
    IQueryable<Terminal> Query();
}
