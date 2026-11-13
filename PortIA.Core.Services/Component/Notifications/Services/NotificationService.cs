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
    IValidator<CreateDraftRequest> createValidator,
    IValidator<UpdateDraftRequest> updateValidator,
    IValidator<ApproveNotificationRequest> approveValidator,
    ILogger<NotificationService> logger) : INotificationService
{
    private const string EstadoDraft = "DRAFT";
    private const string EstadoAprobado = "APROBADO";

    private const int CanalEmail = 1;
    private const int CanalTelegram = 2;

    public async Task<Result<NotificationDraftResponse>> CreateDraftAsync(
        CreateDraftRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<NotificationDraftResponse>.ValidationError(validation.ToDictionary());

        // Consultamos la alerta con AsNoTracking para evitar conflictos de EF Core al guardar el log
        var alerta = await unitOfWork.Alertas.Query()
            .AsNoTracking()
            .Include(a => a.Container)
            .Include(a => a.TipoAlerta)
            .Include(a => a.Riesgo)
            .FirstOrDefaultAsync(a => a.Id == request.AlertaId, ct);

        if (alerta is null)
        {
            logger.LogWarning("CreateDraft rechazado: alerta {AlertaId} no encontrada", request.AlertaId);
            return Result<NotificationDraftResponse>.NotFound($"La alerta {request.AlertaId} no existe.");
        }

        // Generar mensaje base desde el template según canal
        var mensajeBase = request.CanalId == CanalEmail
            ? NotificationTemplates.Email(
                alerta.Container.NumeroContainer,
                alerta.TipoAlerta.Descripcion,
                alerta.Riesgo.Descripcion,
                alerta.Mensaje,
                alerta.CreateAt)
            : NotificationTemplates.Telegram(
                alerta.Container.NumeroContainer,
                alerta.TipoAlerta.Descripcion,
                alerta.Riesgo.Descripcion,
                alerta.Mensaje,
                alerta.CreateAt);

            var log = new LogNotificacion
                    {
                        IdAlerta = request.AlertaId,
                        IdCanal = request.CanalId,
                        Destinatario = request.Destinatario,
                        Estado = EstadoDraft 
                    };

        await unitOfWork.LogsNotificacion.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Draft creado (logId={LogId}, alertaId={AlertaId}, canalId={CanalId})",
            log.Id, request.AlertaId, request.CanalId);

        return Result<NotificationDraftResponse>.Success(
            MapToResponse(log, alerta, mensajeBase, EstadoDraft));
    }

    public async Task<Result<NotificationDraftResponse>> UpdateDraftAsync(
        int id, UpdateDraftRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<NotificationDraftResponse>.ValidationError(validation.ToDictionary());

        var log = await unitOfWork.LogsNotificacion.GetByIdAsync(id, ct);
        if (log is null)
        {
            logger.LogWarning("UpdateDraft rechazado: logNotificacion {Id} no encontrado", id);
            return Result<NotificationDraftResponse>.NotFound($"El borrador {id} no existe.");
        }

        var (estadoActual, _) = ParseEstadoConMensaje(log.Estado);
        if (!estadoActual.Equals(EstadoDraft, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("UpdateDraft rechazado: logNotificacion {Id} ya fue aprobado/enviado", id);
            return Result<NotificationDraftResponse>.BusinessError("Solo se pueden modificar borradores en estado DRAFT.");
        }

        log.Estado = BuildEstadoConMensaje(EstadoDraft, request.Mensaje);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Draft actualizado (logId={LogId})", log.Id);

        return Result<NotificationDraftResponse>.Success(
            MapToResponse(log, log.Alerta, request.Mensaje, EstadoDraft));
    }

    public async Task<Result<NotificationDraftResponse>> ApproveAsync(
        int id, int aprobadoPorUserId, ApproveNotificationRequest request, CancellationToken ct)
    {
        var validation = await approveValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<NotificationDraftResponse>.ValidationError(validation.ToDictionary());

        var log = await unitOfWork.LogsNotificacion.GetByIdAsync(id, ct);
        if (log is null)
        {
            logger.LogWarning("Approve rechazado: logNotificacion {Id} no encontrado", id);
            return Result<NotificationDraftResponse>.NotFound($"La notificación {id} no existe.");
        }

        var (estadoActual, _) = ParseEstadoConMensaje(log.Estado);
        if (estadoActual.Equals(EstadoAprobado, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Approve rechazado: logNotificacion {Id} ya está aprobado", id);
            return Result<NotificationDraftResponse>.BusinessError("Esta notificación ya fue aprobada.");
        }

        log.Estado = BuildEstadoConMensaje(EstadoAprobado, request.Mensaje);
        log.AprobadoPor = aprobadoPorUserId;
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Notificación aprobada (logId={LogId}, aprobadoPor={UserId})",
            log.Id, aprobadoPorUserId);

        return Result<NotificationDraftResponse>.Success(
            MapToResponse(log, log.Alerta, request.Mensaje, EstadoAprobado));
    }

    public async Task<Result<List<AlertaIdResponse>>> GetAlertasByUsuarioAsync(
        int usuarioId, CancellationToken ct)
    {
        var alertas = await unitOfWork.Alertas.Query()
            .Where(a => a.Container.AsignadoA == usuarioId)
            .OrderByDescending(a => a.CreateAt)
            .Select(a => new AlertaIdResponse(
                a.Id,
                a.IdContainer,
                a.Container.NumeroContainer,
                a.TipoAlerta.Descripcion,
                a.Riesgo.Descripcion,
                a.CreateAt))
            .ToListAsync(ct);

        return Result<List<AlertaIdResponse>>.Success(alertas);
    }

    private static string BuildEstadoConMensaje(string estado, string mensaje) =>
        $"{estado}||{mensaje}";

    private static (string Estado, string Mensaje) ParseEstadoConMensaje(string raw)
    {
        var idx = raw.IndexOf("||", StringComparison.Ordinal);
        if (idx < 0) return (raw, string.Empty);
        return (raw[..idx], raw[(idx + 2)..]);
    }

    private static NotificationDraftResponse MapToResponse(
        LogNotificacion log,
        Alerta alerta,
        string mensaje,
        string estado)
    {
        var (_, textoMensaje) = ParseEstadoConMensaje(mensaje);
        if (string.IsNullOrEmpty(textoMensaje)) textoMensaje = mensaje;

        return new(
            log.Id,
            alerta.Id,
            alerta.Container?.NumeroContainer ?? string.Empty,
            alerta.TipoAlerta?.Descripcion ?? string.Empty,
            alerta.Riesgo?.Descripcion ?? string.Empty,
            log.Canal?.Tipo ?? string.Empty,
            log.Destinatario,
            textoMensaje,
            ParseEstadoConMensaje(estado).Estado,
            log.CreateAt);
    }
}