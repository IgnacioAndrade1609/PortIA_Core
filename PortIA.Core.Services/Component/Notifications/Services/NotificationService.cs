namespace PortIA.Core.Services.Component.Notifications.Services;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Notifications.DTOs;
using PortIA.Core.Services.Component.Notifications.Interfaces;
using PortIA.Core.Services.Component.Notifications.Templates;

public class NotificationService(
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    ITelegramSender telegramSender,
    IValidator<SendNotificationRequest> sendValidator,
    ILogger<NotificationService> logger) : INotificationService
{
    private const int RiesgoCriticoId = 3;
    private const int CanalEmail = 1;
    private const int CanalTelegram = 2;
    private const string EstadoAprobado = "APROBADO";

    public async Task<Result<List<AlertaPendienteResponse>>> GetAlertasPendientesByUsuarioAsync(int usuarioId, CancellationToken ct)
    {
        var alertas = await unitOfWork.Alertas.Query()
            .Include(a => a.Container)
                .ThenInclude(c => c.Naviera)
            .Include(a => a.TipoAlerta)
            .Include(a => a.Riesgo)
            .Include(a => a.LogsNotificacion)
            .Where(a => a.Container.AsignadoA == usuarioId && a.IdRiesgo == RiesgoCriticoId)
            .OrderByDescending(a => a.CreateAt)
            .ToListAsync(ct);

        var pendientes = new List<AlertaPendienteResponse>();

        foreach (var alerta in alertas)
        {
            var naviera = alerta.Container?.Naviera;

            var emailAprobado = alerta.LogsNotificacion.Any(l =>
                l.IdCanal == CanalEmail &&
                l.Estado.Equals(EstadoAprobado, StringComparison.OrdinalIgnoreCase) &&
                l.AprobadoPor != null);

            if (!emailAprobado)
            {
                pendientes.Add(new AlertaPendienteResponse(
                    AlertaId: alerta.Id,
                    ContainerId: alerta.IdContainer,
                    NumeroContainer: alerta.Container?.NumeroContainer ?? string.Empty,
                    CanalId: CanalEmail,
                    Canal: "EMAIL",
                    TipoAlerta: alerta.TipoAlerta?.Descripcion ?? "DEMURRAGE",
                    NivelRiesgo: alerta.Riesgo?.Descripcion ?? "CRITICAL",
                    Asunto: alerta.Mensaje,
                    Destinatario: naviera?.Email ?? string.Empty,
                    FechaAlerta: alerta.CreateAt
                ));
            }

            var telegramAprobado = alerta.LogsNotificacion.Any(l =>
                l.IdCanal == CanalTelegram &&
                l.Estado.Equals(EstadoAprobado, StringComparison.OrdinalIgnoreCase) &&
                l.AprobadoPor != null);

            if (!telegramAprobado)
            {
                pendientes.Add(new AlertaPendienteResponse(
                    AlertaId: alerta.Id,
                    ContainerId: alerta.IdContainer,
                    NumeroContainer: alerta.Container?.NumeroContainer ?? string.Empty,
                    CanalId: CanalTelegram,
                    Canal: "TELEGRAM",
                    TipoAlerta: alerta.TipoAlerta?.Descripcion ?? "DEMURRAGE",
                    NivelRiesgo: alerta.Riesgo?.Descripcion ?? "CRITICAL",
                    Asunto: alerta.Mensaje,
                    Destinatario: naviera?.Telefono ?? string.Empty,
                    FechaAlerta: alerta.CreateAt
                ));
            }
        }

        return Result<List<AlertaPendienteResponse>>.Success(pendientes);
    }

    public async Task<Result<NotificationDetailResponse>> GetNotificationDetailAsync(int alertaId, int canalId, CancellationToken ct)
    {
        var alerta = await unitOfWork.Alertas.Query()
            .Include(a => a.Container)
                .ThenInclude(c => c.Naviera)
            .Include(a => a.TipoAlerta)
            .Include(a => a.Riesgo)
            .FirstOrDefaultAsync(a => a.Id == alertaId, ct);

        if (alerta is null)
        {
            return Result<NotificationDetailResponse>.NotFound($"No se encontró la alerta con ID {alertaId}.");
        }

        var naviera = alerta.Container?.Naviera;
        var esEmail = canalId == CanalEmail;

        var destinatario = esEmail
            ? naviera?.Email ?? string.Empty
            : naviera?.Telefono ?? string.Empty;

        var canalNombre = esEmail ? "EMAIL" : "TELEGRAM";

        var mensajeSugerido = esEmail
            ? NotificationTemplates.Email(
                alerta.Container?.NumeroContainer ?? string.Empty,
                alerta.TipoAlerta?.Descripcion ?? "DEMURRAGE",
                alerta.Riesgo?.Descripcion ?? "CRITICAL",
                alerta.Mensaje,
                alerta.CreateAt)
            : NotificationTemplates.Telegram(
                alerta.Container?.NumeroContainer ?? string.Empty,
                alerta.TipoAlerta?.Descripcion ?? "DEMURRAGE",
                alerta.Riesgo?.Descripcion ?? "CRITICAL",
                alerta.Mensaje,
                alerta.CreateAt);

        var response = new NotificationDetailResponse(
            AlertaId: alerta.Id,
            ContainerId: alerta.IdContainer,
            Contenedor: alerta.Container?.NumeroContainer ?? string.Empty,
            TipoCanal: canalId,
            NombreCanal: canalNombre,
            Asunto: alerta.Mensaje,
            Destinatario: destinatario,
            Mensaje: mensajeSugerido
        );

        return Result<NotificationDetailResponse>.Success(response);
    }

    public async Task<Result<SendNotificationResponse>> SendAsync(int alertaId, int usuarioId, SendNotificationRequest request, CancellationToken ct)
    {
        var validation = await sendValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<SendNotificationResponse>.ValidationError(validation.ToDictionary());

        var alerta = await unitOfWork.Alertas.Query()
            .Include(a => a.Container)
                .ThenInclude(c => c.Naviera)
            .Include(a => a.LogsNotificacion)
            .FirstOrDefaultAsync(a => a.Id == alertaId, ct);

        if (alerta is null)
        {
            return Result<SendNotificationResponse>.NotFound($"No se encontró la alerta con ID {alertaId}.");
        }

        var naviera = alerta.Container?.Naviera;

        var destinatario = !string.IsNullOrWhiteSpace(request.Destinatario)
            ? request.Destinatario
            : (request.CanalId == CanalEmail ? naviera?.Email : naviera?.Telefono);

        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return Result<SendNotificationResponse>.BusinessError(
                $"No se encontró un destinatario válido para el canal {(request.CanalId == CanalEmail ? "EMAIL" : "TELEGRAM")}.");
        }

        var asunto = !string.IsNullOrWhiteSpace(request.Asunto)
            ? request.Asunto
            : alerta.Mensaje;

        try
        {
            if (request.CanalId == CanalEmail)
            {
                await emailSender.SendEmailAsync(destinatario, asunto, request.Mensaje, ct);
            }
            else
            {
                await telegramSender.SendMessageAsync(destinatario, request.Mensaje, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo al enviar notificación por canal {CanalId} a {Destinatario}", request.CanalId, destinatario);
            return Result<SendNotificationResponse>.BusinessError($"Fallo al enviar la notificación: {ex.Message}");
        }

        var logExistente = alerta.LogsNotificacion.FirstOrDefault(l =>
            l.IdCanal == request.CanalId &&
            !l.Estado.Equals(EstadoAprobado, StringComparison.OrdinalIgnoreCase));

        int logId;
        if (logExistente is not null)
        {
            logExistente.Estado = EstadoAprobado;
            logExistente.AprobadoPor = usuarioId;
            logExistente.Destinatario = destinatario;
            logId = logExistente.Id;
        }
        else
        {
            var nuevoLog = new LogNotificacion
            {
                IdAlerta = alertaId,
                IdCanal = request.CanalId,
                Destinatario = destinatario,
                Estado = EstadoAprobado,
                AprobadoPor = usuarioId,
                CreateAt = DateTimeOffset.UtcNow
            };
            await unitOfWork.LogsNotificacion.AddAsync(nuevoLog, ct);
            await unitOfWork.SaveChangesAsync(ct);
            logId = nuevoLog.Id;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Notificación enviada y aprobada (alertaId={AlertaId}, canal={CanalId}, logId={LogId}, aprobadoPor={UserId})",
            alertaId, request.CanalId, logId, usuarioId);

        var response = new SendNotificationResponse(
            LogId: logId,
            AlertaId: alertaId,
            CanalId: request.CanalId,
            Canal: request.CanalId == CanalEmail ? "EMAIL" : "TELEGRAM",
            Destinatario: destinatario,
            Estado: EstadoAprobado,
            AprobadoPor: usuarioId,
            FechaEnvio: DateTimeOffset.UtcNow
        );

        return Result<SendNotificationResponse>.Success(response);
    }
}