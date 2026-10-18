namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface ICargaDocumentosRepository
{
    Task AddAsync(CargaDocumentos entity, CancellationToken ct);
    Task<CargaDocumentos?> GetByIdForUserAsync(int id, int userId, CancellationToken ct);
    IQueryable<CargaDocumentos> Query();
}
