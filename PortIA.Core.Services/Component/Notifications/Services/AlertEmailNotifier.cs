namespace PortIA.Core.Services.Component.Notifications.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Alerts.DTOs;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.Services.Component.Notifications.DTOs;
using PortIA.Core.Services.Component.Notifications.Interfaces;
using PortIA.Core.Services.Component.Notifications.Templates;

public class AlertEmailNotifier(
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    ILogger<AlertEmailNotifier> logger) : IAlertNotifier
{
    private const int CanalEmail = 1;
    private static readonly string[] NivelesConCorreo = ["CRITICAL", "OVERDUE"];

    public async Task NotifyAsync(int userId, AlertResponse alert, CancellationToken ct)
    {
        if (!NivelesConCorreo.Contains(alert.NivelRiesgo))
        {
            return;
        }

        try
        {
            await EnviarCorreosAsync(userId, alert, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "No se pudieron procesar los correos automáticos de la alerta {AlertId}; la alerta en tiempo real no se ve afectada.",
                alert.Id);
        }
    }

    private async Task EnviarCorreosAsync(int userId, AlertResponse alert, CancellationToken ct)
    {
        var alerta = await unitOfWork.Alertas.Query()
            .Include(a => a.Container)
                .ThenInclude(c => c.Naviera)
            .Include(a => a.Container)
                .ThenInclude(c => c.Asignado)
            .Include(a => a.TipoAlerta)
            .Include(a => a.Riesgo)
            .FirstOrDefaultAsync(a => a.Id == alert.Id, ct);

        if (alerta is null) return;

        var analista = await unitOfWork.Usuarios.GetByIdWithRoleAsync(userId, ct);
        if (analista is null) return;

        var destinatarios = new List<Usuario> { analista };
        if (analista.JefeId is int jefeId)
        {
            var jefe = await unitOfWork.Usuarios.GetByIdWithRoleAsync(jefeId, ct);
            if (jefe is not null && jefe.Vigente) destinatarios.Add(jefe);
        }

        var container = alerta.Container;
        var tipo = alerta.TipoAlerta?.Descripcion ?? alert.TipoAlerta;
        var contexto = new NotificationContext(
            NumeroContainer: container?.NumeroContainer ?? alert.NumeroContainer,
            TipoAlerta: tipo,
            NivelRiesgo: alert.NivelRiesgo,
            MensajeAlerta: alert.Mensaje,
            FechaAlerta: alert.CreateAt,
            NombreNaviera: container?.Naviera?.NombreNaviera,
            NumeroBl: container?.NumeroBl,
            PuertoDestino: container?.PuertoDestino,
            DeadlineActivo: tipo == "DETENTION" ? container?.DeadlineDetention : container?.DeadlineDemurrage,
            NombreAnalista: container?.Asignado?.NombreUsuario);

        var asunto = NotificationTemplates.AsuntoAnalista(contexto);

        var unicos = destinatarios.DistinctBy(u => u.Email).ToList();
        var correos = unicos.Select(u => u.Email).ToList();
        var copia = unicos.Skip(1).Select(u => u.Email).ToList();

        var estado = NotificationStates.EnviadoAutomatico;
        try
        {
            var cuerpo = NotificationTemplates.EmailAnalista(contexto, analista.NombreUsuario);
            await emailSender.SendEmailAsync(analista.Email, copia, asunto, cuerpo, ct);
        }
        catch (Exception ex)
        {
            estado = NotificationStates.FallidoAutomatico;
            logger.LogWarning(ex,
                "Falló el correo automático de la alerta {AlertId} a {Destinatarios}",
                alert.Id, string.Join(", ", correos));
        }

        foreach (var destinatario in unicos)
        {
            await unitOfWork.LogsNotificacion.AddAsync(new LogNotificacion
            {
                IdAlerta = alert.Id,
                IdCanal = CanalEmail,
                Destinatario = destinatario.Email,
                Estado = estado,
                AprobadoPor = null,
                CreateAt = DateTimeOffset.UtcNow
            }, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
