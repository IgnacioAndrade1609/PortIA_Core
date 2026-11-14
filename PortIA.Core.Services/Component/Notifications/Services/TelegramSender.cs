namespace PortIA.Core.Services.Component.Notifications.Services;

using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortIA.Core.Services.Component.Notifications.Interfaces;

public class TelegramSender(
    IConfiguration configuration,
    ILogger<TelegramSender> logger) : ITelegramSender
{
    private static readonly HttpClient HttpClient = new();

    public async Task SendMessageAsync(string destinatario, string mensaje, CancellationToken ct)
    {
        var botToken = configuration["Telegram:BotToken"];

        // Si aún no se configuró el token, simulamos el envío
        if (string.IsNullOrWhiteSpace(botToken))
        {
            logger.LogWarning("Telegram simulación: BotToken no configurado en AppSettings.json. Mensaje para chat_id '{ChatId}' simulado con éxito.", destinatario);
            return;
        }

        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
        var payload = new
        {
            chat_id = destinatario,
            text = mensaje
        };

        var response = await HttpClient.PostAsJsonAsync(url, payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Error al enviar mensaje Telegram: {StatusCode} - {Error}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Error al enviar mensaje por Telegram: {response.StatusCode}");
        }

        logger.LogInformation("Mensaje de Telegram enviado exitosamente a chat_id {ChatId}", destinatario);
    }
}