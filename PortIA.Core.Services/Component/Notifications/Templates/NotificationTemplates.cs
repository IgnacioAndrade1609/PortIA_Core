namespace PortIA.Core.Services.Component.Notifications.Templates;

public record NotificationContext(
    string NumeroContainer,
    string TipoAlerta,
    string NivelRiesgo,
    string MensajeAlerta,
    DateTimeOffset FechaAlerta,
    string? NombreNaviera,
    string? NumeroBl,
    string? PuertoDestino,
    DateTimeOffset? DeadlineActivo,
    string? NombreAnalista);

public static class NotificationTemplates
{
    private const string Demurrage = "DEMURRAGE";

    public static string AsuntoAnalista(NotificationContext c) =>
        $"[PortIA] {NivelEtiqueta(c.NivelRiesgo)} - Contenedor {c.NumeroContainer}: {TipoEtiqueta(c.TipoAlerta)}";

    public static string AsuntoNaviera(NotificationContext c)
    {
        var prefijo = c.NivelRiesgo == "OVERDUE" ? "URGENTE - " : string.Empty;
        return $"{prefijo}Solicitud de extensión de días libres ({TipoEtiqueta(c.TipoAlerta).ToLowerInvariant()}) - Contenedor {c.NumeroContainer}";
    }

    public static string EmailAnalista(NotificationContext c, string nombreDestinatario)
    {
        return $"""
            Hola {nombreDestinatario},

            El contenedor {c.NumeroContainer}{(string.IsNullOrWhiteSpace(c.NombreNaviera) ? string.Empty : $" ({c.NombreNaviera})")} se encuentra {EstadoFrase(c.NivelRiesgo)} por {TipoEtiqueta(c.TipoAlerta).ToLowerInvariant()}.

            - Detalle: {c.MensajeAlerta}
            - Fecha límite: {FormatearFecha(c.DeadlineActivo)}
            - Analista asignado: {c.NombreAnalista ?? "Sin asignar"}
            - Alerta generada: {c.FechaAlerta:dd/MM/yyyy HH:mm} UTC

            Acción sugerida: {AccionSugerida(c)}

            Puedes revisar el contenedor y gestionar el mensaje a la naviera desde PortIA, en la sección Aprobación de Mensaje.

            Sistema PortIA
            """;
    }

    public static string EmailNaviera(NotificationContext c)
    {
        var naviera = string.IsNullOrWhiteSpace(c.NombreNaviera) ? "equipo" : c.NombreNaviera;
        var bl = string.IsNullOrWhiteSpace(c.NumeroBl) ? string.Empty : $", correspondiente al BL {c.NumeroBl}";
        var puerto = string.IsNullOrWhiteSpace(c.PuertoDestino) ? string.Empty : $", con destino {c.PuertoDestino}";

        return $"""
            Estimados {naviera}:

            Nos comunicamos por el contenedor {c.NumeroContainer}{bl}{puerto}.

            {ParrafoNaviera(c)}

            {SolicitudNaviera(c)}

            Quedamos atentos a su respuesta.

            Saludos cordiales,
            {c.NombreAnalista ?? "Equipo de Comercio Exterior"}
            Gestión de Comercio Exterior - PortIA
            """;
    }

    public static string TelegramNaviera(NotificationContext c)
    {
        var bl = string.IsNullOrWhiteSpace(c.NumeroBl) ? string.Empty : $"\nBL: {c.NumeroBl}";
        return $"""
            Contenedor {c.NumeroContainer}{bl}
            {EncabezadoTelegram(c)}
            Fecha límite: {FormatearFecha(c.DeadlineActivo)}

            {SolicitudNaviera(c)}
            """;
    }

    private static string EncabezadoTelegram(NotificationContext c) => c.NivelRiesgo switch
    {
        "OVERDUE" => $"URGENTE: plazo de {TipoEtiqueta(c.TipoAlerta).ToLowerInvariant()} vencido.",
        "CRITICAL" => $"Plazo de {TipoEtiqueta(c.TipoAlerta).ToLowerInvariant()} próximo a vencer.",
        _ => $"Aviso preventivo de {TipoEtiqueta(c.TipoAlerta).ToLowerInvariant()}."
    };

    private static string ParrafoNaviera(NotificationContext c)
    {
        var fecha = FormatearFecha(c.DeadlineActivo);
        var plazo = c.TipoAlerta == Demurrage
            ? "el plazo de días libres de demurrage"
            : "el plazo de días libres de detention";

        return c.NivelRiesgo switch
        {
            "OVERDUE" => $"Le informamos que {plazo} de este contenedor venció el {fecha} y a la fecha el contenedor sigue sin resolverse.",
            "CRITICAL" => $"Le informamos que {plazo} de este contenedor vence el {fecha} y se encuentra en un punto crítico.",
            _ => $"Le informamos que {plazo} de este contenedor vence el {fecha}, por lo que queremos anticiparnos a una posible extensión."
        };
    }

    private static string SolicitudNaviera(NotificationContext c) => c.TipoAlerta == Demurrage
        ? "Solicitamos evaluar una extensión de los días libres de demurrage y, si corresponde, indicarnos las condiciones y el costo asociado, así como los documentos pendientes para el canje."
        : "Solicitamos evaluar una extensión de los días libres de detention y, si corresponde, indicarnos las condiciones y el costo asociado, así como el depósito habilitado para la devolución del contenedor vacío.";

    private static string AccionSugerida(NotificationContext c) => c.TipoAlerta == Demurrage
        ? "coordina el retiro del contenedor desde el terminal antes de que venza el plazo, o solicita a la naviera una extensión de días libres de demurrage."
        : "coordina la devolución del contenedor vacío antes de que venza el plazo, o solicita a la naviera una extensión de días libres de detention.";

    private static string EstadoFrase(string nivel) => nivel switch
    {
        "OVERDUE" => "con el plazo vencido",
        "CRITICAL" => "en estado crítico, próximo a vencer,",
        _ => "en advertencia"
    };

    private static string NivelEtiqueta(string nivel) => nivel switch
    {
        "OVERDUE" => "Vencido",
        "CRITICAL" => "Crítico",
        "WARNING" => "Advertencia",
        _ => nivel
    };

    private static string TipoEtiqueta(string tipo) => tipo == Demurrage ? "Demurrage" : "Detention";

    private static string FormatearFecha(DateTimeOffset? fecha) =>
        fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy HH:mm") + " UTC" : "sin fecha registrada";
}
