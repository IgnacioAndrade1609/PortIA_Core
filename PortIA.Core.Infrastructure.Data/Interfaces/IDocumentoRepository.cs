namespace PortIA.Core.Infrastructure.Data.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;

public interface IDocumentoRepository
{
    Task AddAsync(Documento entity, CancellationToken ct);
    Task<Documento?> GetByHashAsync(string huellaHash, CancellationToken ct);
    IQueryable<Documento> Query();
}
