namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class UsuarioRepository(PortIACoreDbContext dbContext) : IUsuarioRepository
{
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct) =>
        dbContext.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) =>
        dbContext.Usuarios.AnyAsync(u => u.Email == email, ct);

    public Task AddAsync(Usuario usuario, CancellationToken ct)
    {
        dbContext.Usuarios.Add(usuario);
        return Task.CompletedTask;
    }
}
