namespace PortIA.Core.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;

public class CargaDocumentosRepository(PortIACoreDbContext dbContext) : ICargaDocumentosRepository
{
    public Task AddAsync(CargaDocumentos entity, CancellationToken ct)
    {
        dbContext.CargasDocumentos.Add(entity);
        return Task.CompletedTask;
    }

    public Task<CargaDocumentos?> GetByIdForUserAsync(int id, int userId, CancellationToken ct) =>
        dbContext.CargasDocumentos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.Vigente, ct);

    public IQueryable<CargaDocumentos> Query() =>
        dbContext.CargasDocumentos.AsNoTracking().Where(c => c.Vigente);
}
