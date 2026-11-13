namespace PortIA.Core.Services.Component.Notifications.DTOs;


public class CreateDraftRequest
{
    public int AlertaId { get; set; }

    public int CanalId { get; set; }

    public string Destinatario { get; set; } = null!;
}

public class UpdateDraftRequest
{
    public string Mensaje { get; set; } = null!;
}

public class ApproveNotificationRequest
{
    public string Mensaje { get; set; } = null!;
}


public record NotificationDraftResponse(
    int Id,
    int AlertaId,
    string NumeroContainer,
    string TipoAlerta,
    string NivelRiesgo,
    string Canal,
    string Destinatario,
    string Mensaje,
    string Estado,
    DateTimeOffset CreadoEn
);

public record AlertaIdResponse(
    int AlertaId,
    int ContainerId,
    string NumeroContainer,
    string TipoAlerta,
    string NivelRiesgo,
    DateTimeOffset CreadoEn
);