namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct);
    Task<Usuario?> GetByIdWithRoleAsync(int id, CancellationToken ct);
    Task<(IReadOnlyList<Usuario> Items, long TotalItems)> GetPagedAsync(string? search, int? roleId, bool? active, int page, int pageSize, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    Task AddAsync(Usuario usuario, CancellationToken ct);
}