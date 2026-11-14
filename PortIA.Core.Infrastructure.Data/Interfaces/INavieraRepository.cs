namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface INavieraRepository
{
    Task<Naviera?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<Naviera>> GetAllAsync(CancellationToken ct);
    Task<bool> ExistsByNombreAsync(string nombre, int? excludeId, CancellationToken ct);
    Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct);
    Task AddAsync(Naviera naviera, CancellationToken ct);
    IQueryable<Naviera> Query();
}
