namespace PortIA.Core.Tests.Component.Audit;

using FluentValidation;
using FluentValidation.Results;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Audit.DTOs;
using PortIA.Core.Services.Component.Audit.Services;

public class AuditServiceTests
{
    private readonly ILogAuditoriaRepository _logAuditoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AuditQueryFilter> _validator;
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        _logAuditoriaRepository = Substitute.For<ILogAuditoriaRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.LogsAuditoria.Returns(_logAuditoriaRepository);

        _validator = Substitute.For<IValidator<AuditQueryFilter>>();
        _validator.ValidateAsync(Arg.Any<AuditQueryFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _sut = new AuditService(_unitOfWork, _validator);

        var emptyLogs = new List<LogAuditoria>().BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(emptyLogs);

        var containerRepository = Substitute.For<IContainerRepository>();
        var emptyContainers = new List<Container>().BuildMockDbSet();
        containerRepository.Query().Returns(emptyContainers);
        _unitOfWork.Containers.Returns(containerRepository);

        var alertaRepository = Substitute.For<IAlertaRepository>();
        var emptyAlertas = new List<Alerta>().BuildMockDbSet();
        alertaRepository.Query().Returns(emptyAlertas);
        _unitOfWork.Alertas.Returns(alertaRepository);

        var logNotificacionRepository = Substitute.For<ILogNotificacionRepository>();
        var emptyNotificaciones = new List<LogNotificacion>().BuildMockDbSet();
        logNotificacionRepository.Query().Returns(emptyNotificaciones);
        _unitOfWork.LogsNotificacion.Returns(logNotificacionRepository);
    }

    private static LogAuditoria BuildLog(int id, int? responsable, string entidad = "Container", string accion = "ACTUALIZAR") => new()
    {
        Id = id,
        NombreEntidad = entidad,
        IdEntidad = 1,
        Responsable = responsable,
        ResponsableUsuario = responsable.HasValue ? new Usuario { Id = responsable.Value, NombreUsuario = $"Usuario {responsable}" } : null,
        Accion = new Accion { Id = 1, Descripcion = accion },
        CreateAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task GetPagedAsync_WithInvalidFilter_ReturnsValidationError()
    {
        _validator.ValidateAsync(Arg.Any<AuditQueryFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("FromDate", "inválido") }));

        var result = await _sut.GetPagedAsync(new AuditQueryFilter(), callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
    }

    [Fact]
    public async Task GetPagedAsync_AsAdmin_SeesAllResponsables()
    {
        var logs = new List<LogAuditoria> { BuildLog(1, responsable: 7), BuildLog(2, responsable: 99) };
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var result = await _sut.GetPagedAsync(new AuditQueryFilter(), callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_AsNonAdmin_OnlySeesOwnLogs_EvenIfAnotherResponsableIdRequested()
    {
        var logs = new List<LogAuditoria> { BuildLog(1, responsable: 7), BuildLog(2, responsable: 99) };
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var filter = new AuditQueryFilter { ResponsableId = 99 };
        var result = await _sut.GetPagedAsync(filter, callerUserId: 7, callerRole: "Analista", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = Assert.Single(result.Value.Items);
        Assert.Equal(7, log.ResponsableId);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByEntidadYAccion()
    {
        var logs = new List<LogAuditoria>
        {
            BuildLog(1, responsable: 7, entidad: "Container", accion: "ACTUALIZAR"),
            BuildLog(2, responsable: 7, entidad: "Usuario", accion: "CREAR"),
        };
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var filter = new AuditQueryFilter { NombreEntidad = "Usuario", Accion = "CREAR" };
        var result = await _sut.GetPagedAsync(filter, callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = Assert.Single(result.Value.Items);
        Assert.Equal(2, log.Id);
    }

    [Fact]
    public async Task GetPagedAsync_PaginatesResults()
    {
        var logs = Enumerable.Range(1, 5).Select(i => BuildLog(i, responsable: 1)).ToList();
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var filter = new AuditQueryFilter { Page = 2, PageSize = 2 };
        var result = await _sut.GetPagedAsync(filter, callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(5, result.Value.TotalItems);
        Assert.Equal(3, result.Value.TotalPages);
    }

    [Fact]
    public async Task GetPagedAsync_WithOrigenSistema_ReturnsOnlyLogsWithoutResponsable()
    {
        var logs = new List<LogAuditoria> { BuildLog(1, responsable: 7), BuildLog(2, responsable: null) };
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var result = await _sut.GetPagedAsync(new AuditQueryFilter { Origen = "SISTEMA" }, callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var unico = Assert.Single(result.Value.Items);
        Assert.Equal(2, unico.Id);
    }

    [Fact]
    public async Task GetPagedAsync_WithSoloSensibles_ReturnsOnlySensitiveLogs()
    {
        var sensible = BuildLog(1, responsable: 7, entidad: "Notificacion", accion: "CREAR");
        sensible.ValoresNuevos = "{\"Estado\":\"RECHAZADO\"}";
        var normal = BuildLog(2, responsable: 7, entidad: "Notificacion", accion: "CREAR");
        normal.ValoresNuevos = "{\"Estado\":\"ENVIADO\"}";
        var mockLogs = new List<LogAuditoria> { sensible, normal }.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var result = await _sut.GetPagedAsync(new AuditQueryFilter { SoloSensibles = true }, callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var unico = Assert.Single(result.Value.Items);
        Assert.Equal(1, unico.Id);
        Assert.Equal("MENSAJE_RECHAZADO", unico.Sensibilidad);
    }

    [Fact]
    public async Task GetSummaryAsync_ByNonAdmin_ReturnsUnauthorized()
    {
        var result = await _sut.GetSummaryAsync(new AuditQueryFilter(), callerUserId: 1, callerRole: "Jefe", CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsEventsUsersSystemAndSensitive()
    {
        var rechazo = BuildLog(1, responsable: 7, entidad: "Notificacion", accion: "CREAR");
        rechazo.ValoresNuevos = "{\"Estado\":\"RECHAZADO\"}";
        var logs = new List<LogAuditoria>
        {
            rechazo,
            BuildLog(2, responsable: 7),
            BuildLog(3, responsable: 8),
            BuildLog(4, responsable: null)
        };
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var result = await _sut.GetSummaryAsync(new AuditQueryFilter(), callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.TotalEventos);
        Assert.Equal(2, result.Value.UsuariosActivos);
        Assert.Equal(1, result.Value.EventosDeSistema);
        Assert.Equal(1, result.Value.AccionesSensibles);
    }
}
