namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class RolRepository(PortIACoreDbContext dbContext) : IRolRepository
{
    public Task<Rol?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id && r.Vigente, ct);
}
