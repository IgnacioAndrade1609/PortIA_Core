namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class EstadoContainerRepository(PortIACoreDbContext dbContext) : IEstadoContainerRepository
{
    public Task<EstadoContainer?> GetByDescripcionAsync(string descripcion, CancellationToken ct) =>
        dbContext.EstadosContainer.FirstOrDefaultAsync(e => e.Descripcion == descripcion, ct);
}
