namespace PortIA.Core.Services.Component.Notifications.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Notifications.DTOs;

public interface INotificationService
{

    Task<Result<List<AlertaPendienteResponse>>> GetAlertasPendientesByUsuarioAsync(int usuarioId, CancellationToken ct);

    Task<Result<NotificationDetailResponse>> GetNotificationDetailAsync(int alertaId, int canalId, CancellationToken ct);

    Task<Result<SendNotificationResponse>> SendAsync(int alertaId, int usuarioId, SendNotificationRequest request, CancellationToken ct);
}