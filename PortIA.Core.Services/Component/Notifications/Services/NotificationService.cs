namespace PortIA.Core.Services.Component.Notifications.Services;

using System.Text.Json;
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
    IValidator<RejectNotificationRequest> rejectValidator,
    ILogger<NotificationService> logger) : INotificationService
{
    private const string RiesgoOk = "OK";
    private const int CanalEmail = 1;
    private const int CanalTelegram = 2;
    private const string AccionCrear = "CREAR";
    private const string EntidadNotificacion = "Notificacion";

    public async Task<Result<List<AlertaPendienteResponse>>> GetAlertasPendientesByUsuarioAsync(int usuarioId, CancellationToken ct)
    {
        var alertas = await unitOfWork.Alertas.Query()
            .Include(a => a.Container)
                .ThenInclude(c => c.Naviera)
            .Include(a => a.Container)
                .ThenInclude(c => c.Asignado)
            .Include(a => a.TipoAlerta)
            .Include(a => a.Riesgo)
            .Include(a => a.LogsNotificacion)
            .Where(a => a.Container.AsignadoA == usuarioId && a.Riesgo.Descripcion != RiesgoOk)
            .OrderByDescending(a => a.CreateAt)
            .ToListAsync(ct);

        var pendientes = new List<AlertaPendienteResponse>();

        foreach (var alerta in alertas)
        {
            var naviera = alerta.Container?.Naviera;
            var asunto = NotificationTemplates.AsuntoNaviera(BuildContext(alerta));

            foreach (var canalId in new[] { CanalEmail, CanalTelegram })
            {
                if (EstaResuelta(alerta, canalId)) continue;

                pendientes.Add(new AlertaPendienteResponse(
                    AlertaId: alerta.Id,
                    ContainerId: alerta.IdContainer,
                    NumeroContainer: alerta.Container?.NumeroContainer ?? string.Empty,
                    CanalId: canalId,
                    Canal: canalId == CanalEmail ? "EMAIL" : "TELEGRAM",
                    TipoAlerta: alerta.TipoAlerta?.Descripcion ?? "DEMURRAGE",
                    NivelRiesgo: alerta.Riesgo?.Descripcion ?? "WARNING",
                    Asunto: asunto,
                    Destinatario: DestinatarioPorDefecto(naviera, canalId) ?? string.Empty,
                    FechaAlerta: alerta.CreateAt
                ));
            }
        }

        return Result<List<AlertaPendienteResponse>>.Success(pendientes);
    }

    public async Task<Result<NotificationDetailResponse>> GetNotificationDetailAsync(int alertaId, int usuarioId, int canalId, CancellationToken ct)
    {
        var alerta = await CargarAlertaAsync(alertaId, usuarioId, ct);

        if (alerta is null)
        {
            return Result<NotificationDetailResponse>.NotFound($"La alerta {alertaId} no existe o no pertenece al usuario.");
        }

        var contexto = BuildContext(alerta);
        var esEmail = canalId == CanalEmail;

        var response = new NotificationDetailResponse(
            AlertaId: alerta.Id,
            ContainerId: alerta.IdContainer,
            Contenedor: alerta.Container?.NumeroContainer ?? string.Empty,
            TipoCanal: canalId,
            NombreCanal: esEmail ? "EMAIL" : "TELEGRAM",
            Asunto: NotificationTemplates.AsuntoNaviera(contexto),
            Destinatario: DestinatarioPorDefecto(alerta.Container?.Naviera, canalId) ?? string.Empty,
            Mensaje: esEmail ? NotificationTemplates.EmailNaviera(contexto) : NotificationTemplates.TelegramNaviera(contexto)
        );

        return Result<NotificationDetailResponse>.Success(response);
    }

    public async Task<Result<SendNotificationResponse>> SendAsync(int alertaId, int usuarioId, SendNotificationRequest request, CancellationToken ct)
    {
        var validation = await sendValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<SendNotificationResponse>.ValidationError(validation.ToDictionary());

        var alerta = await CargarAlertaAsync(alertaId, usuarioId, ct);

        if (alerta is null)
        {
            return Result<SendNotificationResponse>.NotFound($"La alerta {alertaId} no existe o no pertenece al usuario.");
        }

        var canalNombre = request.CanalId == CanalEmail ? "EMAIL" : "TELEGRAM";

        if (EstaResuelta(alerta, request.CanalId))
        {
            return Result<SendNotificationResponse>.BusinessError(
                $"El mensaje por {canalNombre} de esta alerta ya fue enviado o rechazado.");
        }

        var destinatario = !string.IsNullOrWhiteSpace(request.Destinatario)
            ? request.Destinatario.Trim()
            : DestinatarioPorDefecto(alerta.Container?.Naviera, request.CanalId);

        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return Result<SendNotificationResponse>.BusinessError(
                $"No se encontró un destinatario válido para el canal {canalNombre}.");
        }

        var asunto = !string.IsNullOrWhiteSpace(request.Asunto)
            ? request.Asunto
            : NotificationTemplates.AsuntoNaviera(BuildContext(alerta));

        var aprobador = usuarioId;

        string? errorEnvio = null;
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
            errorEnvio = ex.Message;
            logger.LogWarning(ex,
                "Fallo el envío por {Canal} (alertaId={AlertaId}, destinatario={Destinatario}). Se registra como FALLIDO y la alerta sigue pendiente.",
                canalNombre, alertaId, destinatario);
        }

        var estado = errorEnvio is null ? NotificationStates.Enviado : NotificationStates.Fallido;
        var log = await RegistrarLogAsync(alertaId, request.CanalId, destinatario, estado, aprobador, ct);

        await RegistrarAuditoriaAsync(log, aprobador, new
        {
            log.Estado,
            Canal = canalNombre,
            log.Destinatario,
            Asunto = asunto,
            Error = errorEnvio
        }, ct);

        if (errorEnvio is not null)
        {
            return Result<SendNotificationResponse>.BusinessError(
                $"No se pudo enviar el mensaje por {canalNombre} a {destinatario}. Revisa el destinatario e inténtalo de nuevo.");
        }

        logger.LogInformation(
            "Notificación enviada (alertaId={AlertaId}, canal={CanalId}, logId={LogId}, aprobadoPor={UserId})",
            alertaId, request.CanalId, log.Id, aprobador);

        return Result<SendNotificationResponse>.Success(ToResponse(log, canalNombre));
    }

    public async Task<Result<SendNotificationResponse>> RejectAsync(int alertaId, int usuarioId, RejectNotificationRequest request, CancellationToken ct)
    {
        var validation = await rejectValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<SendNotificationResponse>.ValidationError(validation.ToDictionary());

        var alerta = await CargarAlertaAsync(alertaId, usuarioId, ct);

        if (alerta is null)
        {
            return Result<SendNotificationResponse>.NotFound($"La alerta {alertaId} no existe o no pertenece al usuario.");
        }

        var canalNombre = request.CanalId == CanalEmail ? "EMAIL" : "TELEGRAM";

        if (EstaResuelta(alerta, request.CanalId))
        {
            return Result<SendNotificationResponse>.BusinessError(
                $"El mensaje por {canalNombre} de esta alerta ya fue enviado o rechazado.");
        }

        var destinatario = DestinatarioPorDefecto(alerta.Container?.Naviera, request.CanalId) ?? "Sin destinatario";
        var aprobador = usuarioId;

        var log = await RegistrarLogAsync(alertaId, request.CanalId, destinatario, NotificationStates.Rechazado, aprobador, ct);

        await RegistrarAuditoriaAsync(log, aprobador, new
        {
            log.Estado,
            Canal = canalNombre,
            log.Destinatario,
            request.Motivo
        }, ct);

        logger.LogInformation(
            "Notificación rechazada (alertaId={AlertaId}, canal={CanalId}, logId={LogId}, rechazadoPor={UserId})",
            alertaId, request.CanalId, log.Id, aprobador);

        return Result<SendNotificationResponse>.Success(ToResponse(log, canalNombre));
    }

    private Task<Alerta?> CargarAlertaAsync(int alertaId, int usuarioId, CancellationToken ct) =>
        unitOfWork.Alertas.Query()
            .Include(a => a.Container)
                .ThenInclude(c => c.Naviera)
            .Include(a => a.Container)
                .ThenInclude(c => c.Asignado)
            .Include(a => a.TipoAlerta)
            .Include(a => a.Riesgo)
            .Include(a => a.LogsNotificacion)
            .FirstOrDefaultAsync(a => a.Id == alertaId && a.Container.AsignadoA == usuarioId, ct);

    private static bool EstaResuelta(Alerta alerta, int canalId) =>
        alerta.LogsNotificacion.Any(l =>
            l.IdCanal == canalId &&
            NotificationStates.Resueltos.Contains(l.Estado, StringComparer.OrdinalIgnoreCase));

    private static string? DestinatarioPorDefecto(Naviera? naviera, int canalId) =>
        canalId == CanalEmail ? naviera?.Email : naviera?.Telefono;

    private static NotificationContext BuildContext(Alerta alerta)
    {
        var container = alerta.Container;
        var tipo = alerta.TipoAlerta?.Descripcion ?? "DEMURRAGE";
        var deadline = tipo == "DETENTION" ? container?.DeadlineDetention : container?.DeadlineDemurrage;

        return new NotificationContext(
            NumeroContainer: container?.NumeroContainer ?? string.Empty,
            TipoAlerta: tipo,
            NivelRiesgo: alerta.Riesgo?.Descripcion ?? "WARNING",
            MensajeAlerta: alerta.Mensaje,
            FechaAlerta: alerta.CreateAt,
            NombreNaviera: container?.Naviera?.NombreNaviera,
            NumeroBl: container?.NumeroBl,
            PuertoDestino: container?.PuertoDestino,
            DeadlineActivo: deadline,
            NombreAnalista: container?.Asignado?.NombreUsuario);
    }

    private async Task<LogNotificacion> RegistrarLogAsync(
        int alertaId, int canalId, string destinatario, string estado, int usuarioId, CancellationToken ct)
    {
        var log = new LogNotificacion
        {
            IdAlerta = alertaId,
            IdCanal = canalId,
            Destinatario = destinatario,
            Estado = estado,
            AprobadoPor = usuarioId,
            CreateAt = DateTimeOffset.UtcNow
        };

        await unitOfWork.LogsNotificacion.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return log;
    }

    private async Task RegistrarAuditoriaAsync(LogNotificacion log, int usuarioId, object valoresNuevos, CancellationToken ct)
    {
        var accion = await unitOfWork.Acciones.GetByDescripcionAsync(AccionCrear, ct);
        if (accion is null)
        {
            logger.LogWarning("No se encontró la acción '{Accion}', no se registra auditoría de la notificación {LogId}", AccionCrear, log.Id);
            return;
        }

        await unitOfWork.LogsAuditoria.AddAsync(new LogAuditoria
        {
            NombreEntidad = EntidadNotificacion,
            IdEntidad = log.Id,
            IdAccion = accion.Id,
            Responsable = usuarioId,
            ValoresNuevos = JsonSerializer.Serialize(valoresNuevos)
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }

    private static SendNotificationResponse ToResponse(LogNotificacion log, string canalNombre) => new(
        LogId: log.Id,
        AlertaId: log.IdAlerta,
        CanalId: log.IdCanal,
        Canal: canalNombre,
        Destinatario: log.Destinatario,
        Estado: log.Estado,
        AprobadoPor: log.AprobadoPor ?? 0,
        FechaEnvio: log.CreateAt);
}
