namespace PortIA.Core.Services.Component.Notifications.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct);

    Task SendEmailAsync(string destinatario, IReadOnlyCollection<string> copia, string asunto, string cuerpo, CancellationToken ct);
}
