namespace PortIA.Core.Services.Component.Notifications.Templates;


public static class NotificationTemplates
{
    public static string Email(
        string numeroContainer,
        string tipoAlerta,
        string nivelRiesgo,
        string mensajeAlerta,
        DateTimeOffset fechaAlerta)
    {
        return $"""
            Estimado equipo,

            Se ha generado una alerta en el sistema PortIA que requiere su atención:

            - Contenedor : {numeroContainer}
            - Tipo de alerta : {tipoAlerta}
            - Nivel de riesgo : {nivelRiesgo}
            - Detalle : {mensajeAlerta}
            - Fecha : {fechaAlerta:dd/MM/yyyy HH:mm} UTC

            Por favor, revise el estado del contenedor y tome las acciones correspondientes.

            Atentamente,
            Sistema PortIA
            """;
    }

    public static string Telegram(
        string numeroContainer,
        string tipoAlerta,
        string nivelRiesgo,
        string mensajeAlerta,
        DateTimeOffset fechaAlerta)
    {
        return $"""
             *Alerta PortIA*
             Contenedor: `{numeroContainer}`
             Tipo: {tipoAlerta}
             Riesgo: {nivelRiesgo}
             Detalle: {mensajeAlerta}
             Fecha: {fechaAlerta:dd/MM/yyyy HH:mm} UTC
            """;
    }
}