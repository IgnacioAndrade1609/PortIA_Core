namespace PortIA.Core.Infrastructure.Data.UnitOfWork;

using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.Repositories;

public class UnitOfWork(PortIACoreDbContext dbContext) : IUnitOfWork
{
    private ICargaDocumentosRepository? _cargasDocumentos;
    private IDocumentoRepository? _documentos;
    private IContainerRepository? _containers;
    private IHitoRepository? _hitos;
    private ITerminalRepository? _terminales;
    private IClimaRepository? _climas;
    private IAccionRepository? _acciones;
    private ILogAuditoriaRepository? _logsAuditoria;
    private IUsuarioRepository? _usuarios;
    private IRolRepository? _roles;

    public ICargaDocumentosRepository CargasDocumentos => _cargasDocumentos ??= new CargaDocumentosRepository(dbContext);
    public IDocumentoRepository Documentos => _documentos ??= new DocumentoRepository(dbContext);
    public IContainerRepository Containers => _containers ??= new ContainerRepository(dbContext);
    public IHitoRepository Hitos => _hitos ??= new HitoRepository(dbContext);
    public ITerminalRepository Terminales => _terminales ??= new TerminalRepository(dbContext);
    public IClimaRepository Climas => _climas ??= new ClimaRepository(dbContext);
    public IAccionRepository Acciones => _acciones ??= new AccionRepository(dbContext);
    public ILogAuditoriaRepository LogsAuditoria => _logsAuditoria ??= new LogAuditoriaRepository(dbContext);
    public IUsuarioRepository Usuarios => _usuarios ??= new UsuarioRepository(dbContext);
    public IRolRepository Roles => _roles ??= new RolRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
}
