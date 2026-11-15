namespace PortIA.Core.Tests.Component.Notifications;

using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Notifications.DTOs;
using PortIA.Core.Services.Component.Notifications.Interfaces;
using PortIA.Core.Services.Component.Notifications.Services;

public class NotificationServiceTests
{
    private const int Analista = 10;
    private const int OtroAnalista = 11;
    private const int AlertaId = 5;

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAlertaRepository _alertas = Substitute.For<IAlertaRepository>();
    private readonly ILogNotificacionRepository _logs = Substitute.For<ILogNotificacionRepository>();
    private readonly ILogAuditoriaRepository _auditoria = Substitute.For<ILogAuditoriaRepository>();
    private readonly IAccionRepository _acciones = Substitute.For<IAccionRepository>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly ITelegramSender _telegram = Substitute.For<ITelegramSender>();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _unitOfWork.Alertas.Returns(_alertas);
        _unitOfWork.LogsNotificacion.Returns(_logs);
        _unitOfWork.LogsAuditoria.Returns(_auditoria);
        _unitOfWork.Acciones.Returns(_acciones);
        _acciones.GetByDescripcionAsync("CREAR", Arg.Any<CancellationToken>()).Returns(new Accion { Id = 1, Descripcion = "CREAR" });

        var sendValidator = Substitute.For<IValidator<SendNotificationRequest>>();
        sendValidator.ValidateAsync(Arg.Any<SendNotificationRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var rejectValidator = Substitute.For<IValidator<RejectNotificationRequest>>();
        rejectValidator.ValidateAsync(Arg.Any<RejectNotificationRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());

        _sut = new NotificationService(
            _unitOfWork, _email, _telegram, sendValidator, rejectValidator, Substitute.For<ILogger<NotificationService>>());
    }

    private void ConAlerta(params LogNotificacion[] logs)
    {
        var alerta = new Alerta
        {
            Id = AlertaId,
            IdContainer = 1,
            Mensaje = "Contenedor ABCU1234567: riesgo de DEMURRAGE en nivel CRITICAL.",
            TipoAlerta = new TipoAlerta { Id = 1, Descripcion = "DEMURRAGE" },
            Riesgo = new Riesgo { Id = 3, Descripcion = "CRITICAL" },
            Container = new Container
            {
                Id = 1,
                NumeroContainer = "ABCU1234567",
                AsignadoA = Analista,
                Asignado = new Usuario { Id = Analista, NombreUsuario = "Analista Uno" },
                Naviera = new Naviera { Id = 1, NombreNaviera = "Maersk", Email = "naviera@ejemplo.com", Telefono = "123456" }
            },
            LogsNotificacion = logs.ToList()
        };

        var dbSet = new List<Alerta> { alerta }.BuildMockDbSet();
        _alertas.Query().Returns(dbSet);
    }

    private static SendNotificationRequest EnvioEmail() => new() { CanalId = 1, Mensaje = "Hola", Asunto = "Asunto" };

    [Fact]
    public async Task SendAsync_ByNonOwner_ReturnsNotFoundAndSendsNothing()
    {
        ConAlerta();

        var result = await _sut.SendAsync(AlertaId, OtroAnalista, EnvioEmail(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        await _email.DidNotReceive().SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _logs.DidNotReceive().AddAsync(Arg.Any<LogNotificacion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenProviderFails_RegistersFailedAndReturnsBusinessError()
    {
        ConAlerta();
        _email.SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("smtp caído"));

        var result = await _sut.SendAsync(AlertaId, Analista, EnvioEmail(), CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _logs.Received(1).AddAsync(
            Arg.Is<LogNotificacion>(l => l.Estado == NotificationStates.Fallido && l.AprobadoPor == Analista),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenProviderSucceeds_RegistersSent()
    {
        ConAlerta();

        var result = await _sut.SendAsync(AlertaId, Analista, EnvioEmail(), CancellationToken.None);

        Assert.Equal(ResultStatus.Success, result.Status);
        await _logs.Received(1).AddAsync(
            Arg.Is<LogNotificacion>(l => l.Estado == NotificationStates.Enviado && l.Destinatario == "naviera@ejemplo.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenAlreadySent_ReturnsBusinessErrorWithoutResending()
    {
        ConAlerta(new LogNotificacion { IdCanal = 1, Estado = NotificationStates.Enviado });

        var result = await _sut.SendAsync(AlertaId, Analista, EnvioEmail(), CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _email.DidNotReceive().SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_RegistersRejectedAndAudit()
    {
        ConAlerta();

        var result = await _sut.RejectAsync(AlertaId, Analista, new RejectNotificationRequest { CanalId = 1, Motivo = "no aplica" }, CancellationToken.None);

        Assert.Equal(ResultStatus.Success, result.Status);
        await _logs.Received(1).AddAsync(
            Arg.Is<LogNotificacion>(l => l.Estado == NotificationStates.Rechazado && l.AprobadoPor == Analista),
            Arg.Any<CancellationToken>());
        await _auditoria.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(a => a.NombreEntidad == "Notificacion" && a.Responsable == Analista),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_ByNonOwner_ReturnsNotFound()
    {
        ConAlerta();

        var result = await _sut.RejectAsync(AlertaId, OtroAnalista, new RejectNotificationRequest { CanalId = 1 }, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetAlertasPendientes_ExcludesResolvedChannelsAndOkLevel()
    {
        ConAlerta(new LogNotificacion { IdCanal = 1, Estado = NotificationStates.Enviado });

        var result = await _sut.GetAlertasPendientesByUsuarioAsync(Analista, CancellationToken.None);

        var pendientes = Assert.IsType<List<AlertaPendienteResponse>>(result.Value);
        var unico = Assert.Single(pendientes);
        Assert.Equal(2, unico.CanalId);
    }
}
