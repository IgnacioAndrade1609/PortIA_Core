namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class DocumentoRepository(PortIACoreDbContext dbContext) : IDocumentoRepository
{
    public Task AddAsync(Documento entity, CancellationToken ct)
    {
        dbContext.Documentos.Add(entity);
        return Task.CompletedTask;
    }

    public Task<Documento?> GetByHashAsync(string huellaHash, CancellationToken ct) =>
        dbContext.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.HuellaHash == huellaHash, ct);

    public Task<Documento?> GetByIdAsync(int id, CancellationToken ct) =>
        dbContext.Documentos.FirstOrDefaultAsync(d => d.Id == id, ct);

    public IQueryable<Documento> Query() =>
        dbContext.Documentos.AsNoTracking().Where(d => d.Vigente);
}
