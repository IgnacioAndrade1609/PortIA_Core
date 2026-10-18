namespace PortIA.Core.Infrastructure.Data.UnitOfWork;

using PortIA.Core.Infrastructure.Data.Interfaces;

public interface IUnitOfWork
{
    ICargaDocumentosRepository CargasDocumentos { get; }
    IDocumentoRepository Documentos { get; }
    IContainerRepository Containers { get; }
    IHitoRepository Hitos { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);
}
