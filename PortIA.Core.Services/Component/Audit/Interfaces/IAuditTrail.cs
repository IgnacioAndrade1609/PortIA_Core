namespace PortIA.Core.Services.Component.Audit.Interfaces;

public interface IAuditTrail
{
    Task RecordAsync(string entidad, int idEntidad, string accion, int? responsable, object? anteriores, object? nuevos, CancellationToken ct);

    Task RecordLoginAsync(int usuarioId, CancellationToken ct);

    Task RecordFailedLoginAsync(string email, CancellationToken ct);
}
