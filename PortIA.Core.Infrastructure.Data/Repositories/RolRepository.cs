namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class RolRepository(PortIACoreDbContext context) : IRolRepository
{
    public async Task<Rol?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Roles.FindAsync([id], ct);
    }

    public async Task<IEnumerable<Rol>> GetAllAsync(CancellationToken ct)
    {
        return await context.Roles
            .AsNoTracking()
            .ToListAsync(ct);
    }
}