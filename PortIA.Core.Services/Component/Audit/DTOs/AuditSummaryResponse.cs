namespace PortIA.Core.Services.Component.Audit.DTOs;

public record AuditSummaryResponse(
    int TotalEventos,
    int UsuariosActivos,
    int EventosDeSistema,
    int AccionesSensibles,
    IReadOnlyList<AuditSensitiveCount> SensiblesPorTipo,
    bool ResultadoTruncado
);

public record AuditSensitiveCount(string Tipo, int Cantidad);
