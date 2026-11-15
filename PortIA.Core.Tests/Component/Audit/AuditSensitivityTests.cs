namespace PortIA.Core.Tests.Component.Audit;

using PortIA.Core.Services.Component.Audit.Services;

public class AuditSensitivityTests
{
    [Fact]
    public void Clasificar_ContainerReassignment_ReturnsReasignacion()
    {
        var tipo = AuditSensitivity.Clasificar(
            "Container", "ACTUALIZAR", "{\"Asignado\":\"A\"}", "{\"Asignado\":\"B\"}");

        Assert.Equal(AuditSensitivity.Reasignacion, tipo);
    }

    [Fact]
    public void Clasificar_FreeDaysChanged_ReturnsDiasLibres()
    {
        var tipo = AuditSensitivity.Clasificar(
            "Container", "ACTUALIZAR", "{\"DiasLibresDemurrage\":3}", "{\"DiasLibresDemurrage\":1}");

        Assert.Equal(AuditSensitivity.DiasLibres, tipo);
    }

    [Fact]
    public void Clasificar_ContainerUpdateWithoutRelevantChange_ReturnsNull()
    {
        var tipo = AuditSensitivity.Clasificar(
            "Container", "ACTUALIZAR", "{\"DiasLibresDemurrage\":3}", "{\"DiasLibresDemurrage\":3}");

        Assert.Null(tipo);
    }

    [Theory]
    [InlineData("RECHAZADO", AuditSensitivity.MensajeRechazado)]
    [InlineData("FALLIDO", AuditSensitivity.MensajeFallido)]
    [InlineData("FALLIDO_AUTOMATICO", AuditSensitivity.MensajeFallido)]
    public void Clasificar_NotificationStates_ReturnsExpectedType(string estado, string esperado)
    {
        var tipo = AuditSensitivity.Clasificar("Notificacion", "CREAR", null, $"{{\"Estado\":\"{estado}\"}}");

        Assert.Equal(esperado, tipo);
    }

    [Fact]
    public void Clasificar_SentNotification_ReturnsNull()
    {
        var tipo = AuditSensitivity.Clasificar("Notificacion", "CREAR", null, "{\"Estado\":\"ENVIADO\"}");

        Assert.Null(tipo);
    }

    [Fact]
    public void Clasificar_FailedLogin_ReturnsLoginFallido()
    {
        var tipo = AuditSensitivity.Clasificar("Sesion", "CREAR", null, "{\"Evento\":\"INICIO_SESION_FALLIDO\"}");

        Assert.Equal(AuditSensitivity.LoginFallido, tipo);
    }

    [Fact]
    public void Clasificar_SuccessfulLogin_ReturnsNull()
    {
        var tipo = AuditSensitivity.Clasificar("Sesion", "CREAR", null, "{\"Evento\":\"INICIO_SESION\"}");

        Assert.Null(tipo);
    }

    [Theory]
    [InlineData("Usuario", AuditSensitivity.CambioUsuario)]
    [InlineData("Naviera", AuditSensitivity.CambioCatalogo)]
    [InlineData("Terminal", AuditSensitivity.CambioCatalogo)]
    [InlineData("CredencialTerminal", AuditSensitivity.Credencial)]
    public void Clasificar_AdministrativeEntities_AreSensitive(string entidad, string esperado)
    {
        var tipo = AuditSensitivity.Clasificar(entidad, "ACTUALIZAR", null, "{}");

        Assert.Equal(esperado, tipo);
    }

    [Fact]
    public void Clasificar_WithInvalidJson_DoesNotThrow()
    {
        var tipo = AuditSensitivity.Clasificar("Container", "ACTUALIZAR", "no es json", "tampoco");

        Assert.Null(tipo);
    }
}
