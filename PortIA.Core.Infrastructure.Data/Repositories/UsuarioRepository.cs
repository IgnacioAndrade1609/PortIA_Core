namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class UsuarioRepository(PortIACoreDbContext context) : IUsuarioRepository
{
    public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<Usuario?> GetByIdWithRoleAsync(int id, CancellationToken ct)
    {
        return await context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<(IReadOnlyList<Usuario> Items, long TotalItems)> GetPagedAsync(
        string? search, 
        int? roleId, 
        bool? active, 
        int page, 
        int pageSize, 
        CancellationToken ct)
    {
        var query = context.Usuarios
            .Include(u => u.Rol)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => 
                u.NombreUsuario.Contains(search) || 
                u.Email.Contains(search));
        }

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RolId == roleId.Value);
        }

        if (active.HasValue)
        {
            query = query.Where(u => u.Vigente == active.Value);
        }

        var totalItems = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalItems);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)
    {
        return await context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task AddAsync(Usuario usuario, CancellationToken ct)
    {
        await context.Usuarios.AddAsync(usuario, ct);
    }
}