namespace PortIA.Core.Infrastructure.Data.UnitOfWork;

using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.Repositories;

public class UnitOfWork(PortIACoreDbContext dbContext) : IUnitOfWork
{
    private ICargaDocumentosRepository? _cargasDocumentos;
    private IDocumentoRepository? _documentos;
    private IContainerRepository? _containers;
    private IHitoRepository? _hitos;

    public ICargaDocumentosRepository CargasDocumentos => _cargasDocumentos ??= new CargaDocumentosRepository(dbContext);
    public IDocumentoRepository Documentos => _documentos ??= new DocumentoRepository(dbContext);
    public IContainerRepository Containers => _containers ??= new ContainerRepository(dbContext);
    public IHitoRepository Hitos => _hitos ??= new HitoRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
}
