namespace PortIA.Core.Services.Component.Notifications.Services;

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortIA.Core.Services.Component.Notifications.Interfaces;

public class EmailSender(
    IConfiguration configuration,
    ILogger<EmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct) =>
        SendEmailAsync(destinatario, [], asunto, cuerpo, ct);

    public async Task SendEmailAsync(string destinatario, IReadOnlyCollection<string> copia, string asunto, string cuerpo, CancellationToken ct)
    {
        var host = configuration["Email:SmtpHost"];
        var portStr = configuration["Email:SmtpPort"];
        var sender = configuration["Email:SenderAddress"] ?? "notificaciones@portia.cl";
        var senderName = configuration["Email:SenderName"] ?? "PortIA Sistema";
        var apiKey = configuration["Email:ApiKey"];
        var useSslStr = configuration["Email:UseSsl"];

        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogError("El correo no se envió: Email:SmtpHost no está configurado.");
            throw new InvalidOperationException("El servidor de correo no está configurado (Email:SmtpHost).");
        }

        var port = int.TryParse(portStr, out var p) ? p : 587;
        var useSsl = bool.TryParse(useSslStr, out var ssl) ? ssl : true;

        using var mail = new MailMessage
        {
            From = new MailAddress(sender, senderName),
            Subject = asunto,
            Body = cuerpo,
            IsBodyHtml = false
        };
        mail.To.Add(destinatario);
        foreach (var correoCopia in copia)
        {
            mail.CC.Add(correoCopia);
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = useSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(sender, apiKey)
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.Credentials = new NetworkCredential(sender, apiKey);
        }

        await client.SendMailAsync(mail, ct);
        logger.LogInformation("Correo enviado exitosamente a {Destinatario} (copia: {Copia})", destinatario, string.Join(", ", copia));
    }
}