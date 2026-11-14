namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface ITerminalRepository
{
    Task<Terminal?> GetByIdAsync(int id, CancellationToken ct);
    Task<Terminal?> GetByIdWithPaisAsync(int id, CancellationToken ct);
    Task<List<Terminal>> GetAllWithPaisAsync(CancellationToken ct);
    Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct);
    Task AddAsync(Terminal terminal, CancellationToken ct);
    IQueryable<Terminal> Query();
}
