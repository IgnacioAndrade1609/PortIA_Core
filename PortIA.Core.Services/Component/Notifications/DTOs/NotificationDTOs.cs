namespace PortIA.Core.Services.Component.Notifications.DTOs;

public class SendNotificationRequest
{
    public int CanalId { get; set; } = 1;

    public string Mensaje { get; set; } = null!;

    public string? Destinatario { get; set; }

    public string? Asunto { get; set; }
}

// ─── Responses ──────────────────────────────────────────────────────────────

public record AlertaPendienteResponse(
    int AlertaId,
    int ContainerId,
    string NumeroContainer,
    int CanalId,
    string Canal,
    string TipoAlerta,
    string NivelRiesgo,
    string Asunto,
    string Destinatario,
    DateTimeOffset FechaAlerta
);


public record NotificationDetailResponse(
    int AlertaId,
    int ContainerId,
    string Contenedor,
    int TipoCanal,
    string NombreCanal,
    string Asunto,
    string Destinatario,
    string Mensaje
);


public record SendNotificationResponse(
    int LogId,
    int AlertaId,
    int CanalId,
    string Canal,
    string Destinatario,
    string Estado,
    int AprobadoPor,
    DateTimeOffset FechaEnvio
);