namespace PortIA.Core.Services.Component.Notifications.Interfaces;

public interface ITelegramSender
{
    Task SendMessageAsync(string destinatario, string mensaje, CancellationToken ct);
}