namespace PortIA.Core.Infrastructure.Data.UnitOfWork;

using PortIA.Core.Infrastructure.Data.Interfaces;

public interface IUnitOfWork
{
    ICargaDocumentosRepository CargasDocumentos { get; }
    IDocumentoRepository Documentos { get; }
    IContainerRepository Containers { get; }
    IHitoRepository Hitos { get; }
    ITerminalRepository Terminales { get; }
    IClimaRepository Climas { get; }
    IAccionRepository Acciones { get; }
    ILogAuditoriaRepository LogsAuditoria { get; }
    IUsuarioRepository Usuarios { get; }
    IRolRepository Roles { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);
}
