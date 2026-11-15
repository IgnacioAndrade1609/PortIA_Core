namespace PortIA.Core.Services.Component.Audit.Services;

using System.Text.Json;

public static class AuditSensitivity
{
    public const string Reasignacion = "REASIGNACION";
    public const string DiasLibres = "DIAS_LIBRES";
    public const string MensajeRechazado = "MENSAJE_RECHAZADO";
    public const string MensajeFallido = "MENSAJE_FALLIDO";
    public const string Credencial = "CREDENCIAL";
    public const string CambioUsuario = "CAMBIO_USUARIO";
    public const string CambioCatalogo = "CAMBIO_CATALOGO";
    public const string LoginFallido = "LOGIN_FALLIDO";

    public static string? Clasificar(string entidad, string accion, string? anteriores, string? nuevos)
    {
        var antes = Parsear(anteriores);
        var despues = Parsear(nuevos);

        switch (entidad)
        {
            case "Container" when accion == "ACTUALIZAR":
                if (despues is not null && despues.ContainsKey("Asignado")) return Reasignacion;
                if (Cambio(antes, despues, "DiasLibresDemurrage") || Cambio(antes, despues, "DiasLibresDetention")) return DiasLibres;
                return null;

            case "Notificacion":
                var estado = Texto(despues, "Estado");
                if (estado == "RECHAZADO") return MensajeRechazado;
                if (estado is "FALLIDO" or "FALLIDO_AUTOMATICO") return MensajeFallido;
                return null;

            case "CredencialTerminal":
                return Credencial;

            case "Usuario":
                return accion == "LEER" ? null : CambioUsuario;

            case "Naviera":
            case "Terminal":
                return accion == "LEER" ? null : CambioCatalogo;

            case "Sesion":
                return Texto(despues, "Evento") == "INICIO_SESION_FALLIDO" ? LoginFallido : null;

            default:
                return null;
        }
    }

    private static Dictionary<string, JsonElement>? Parsear(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool Cambio(Dictionary<string, JsonElement>? antes, Dictionary<string, JsonElement>? despues, string clave)
    {
        if (despues is null || !despues.TryGetValue(clave, out var nuevo)) return false;
        if (antes is null || !antes.TryGetValue(clave, out var previo)) return true;
        return previo.GetRawText() != nuevo.GetRawText();
    }

    private static string? Texto(Dictionary<string, JsonElement>? dic, string clave) =>
        dic is not null && dic.TryGetValue(clave, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;
}
