namespace PortIA.Core.Services.Component.Notifications.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Notifications.DTOs;

public interface INotificationService
{
    Task<Result<NotificationDraftResponse>> CreateDraftAsync(CreateDraftRequest request, CancellationToken ct);

    Task<Result<NotificationDraftResponse>> UpdateDraftAsync(int id, UpdateDraftRequest request, CancellationToken ct);

    Task<Result<NotificationDraftResponse>> ApproveAsync(int id, int aprobadoPorUserId, ApproveNotificationRequest request, CancellationToken ct);

    Task<Result<List<AlertaIdResponse>>> GetAlertasByUsuarioAsync(int usuarioId, CancellationToken ct);
}