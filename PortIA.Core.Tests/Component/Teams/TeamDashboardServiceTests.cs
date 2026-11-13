namespace PortIA.Core.Tests.Component.Teams;

using FluentValidation;
using FluentValidation.Results;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Teams.DTOs;
using PortIA.Core.Services.Component.Teams.Services;

public class TeamDashboardServiceTests
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IContainerRepository _containerRepository;
    private readonly IAlertaRepository _alertaRepository;
    private readonly ICargaDocumentosRepository _cargaDocumentosRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<TeamDashboardFilter> _validator;
    private readonly TeamDashboardService _sut;

    public TeamDashboardServiceTests()
    {
        _usuarioRepository = Substitute.For<IUsuarioRepository>();
        _containerRepository = Substitute.For<IContainerRepository>();
        _alertaRepository = Substitute.For<IAlertaRepository>();
        _cargaDocumentosRepository = Substitute.For<ICargaDocumentosRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Usuarios.Returns(_usuarioRepository);
        _unitOfWork.Containers.Returns(_containerRepository);
        _unitOfWork.Alertas.Returns(_alertaRepository);
        _unitOfWork.CargasDocumentos.Returns(_cargaDocumentosRepository);

        _validator = Substitute.For<IValidator<TeamDashboardFilter>>();
        _validator.ValidateAsync(Arg.Any<TeamDashboardFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _sut = new TeamDashboardService(_unitOfWork, _validator);

        var emptyContainers = new List<Container>().BuildMockDbSet();
        _containerRepository.Query().Returns(emptyContainers);
        var emptyAlertas = new List<Alerta>().BuildMockDbSet();
        _alertaRepository.Query().Returns(emptyAlertas);
        var emptyCargas = new List<CargaDocumentos>().BuildMockDbSet();
        _cargaDocumentosRepository.Query().Returns(emptyCargas);
    }

    private static Usuario BuildJefe(int id) => new()
    {
        Id = id,
        NombreUsuario = $"Jefe {id}",
        Rol = new Rol { Id = 2, NombreRol = "Jefe" }
    };

    [Fact]
    public async Task GetDashboardAsync_WithInvalidFilter_ReturnsValidationError()
    {
        _validator.ValidateAsync(Arg.Any<TeamDashboardFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("FromDate", "inválido") }));

        var result = await _sut.GetDashboardAsync(2, callerUserId: 2, callerRole: "Jefe", new TeamDashboardFilter(), CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
    }

    [Fact]
    public async Task GetDashboardAsync_WithUnauthorizedRole_ReturnsUnauthorized()
    {
        var result = await _sut.GetDashboardAsync(2, callerUserId: 1, callerRole: "Analista", new TeamDashboardFilter(), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenJefeNotFound_ReturnsNotFound()
    {
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns((Usuario?)null);

        var result = await _sut.GetDashboardAsync(2, callerUserId: 2, callerRole: "Jefe", new TeamDashboardFilter(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenTargetIsNotJefe_ReturnsBusinessError()
    {
        var noJefe = new Usuario { Id = 2, NombreUsuario = "Camila", Rol = new Rol { Id = 1, NombreRol = "Analista" } };
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(noJefe);

        var result = await _sut.GetDashboardAsync(2, callerUserId: 99, callerRole: "Admin", new TeamDashboardFilter(), CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
    }

    [Fact]
    public async Task GetDashboardAsync_AsJefe_IgnoresRouteJefeIdAndUsesOwnId()
    {
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(BuildJefe(2));
        _usuarioRepository.GetPagedAsync(null, null, 2, null, 1, 1000, Arg.Any<CancellationToken>())
            .Returns((new List<Usuario>(), 0L));

        var result = await _sut.GetDashboardAsync(7, callerUserId: 2, callerRole: "Jefe", new TeamDashboardFilter(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _usuarioRepository.Received(1).GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>());
        await _usuarioRepository.DidNotReceive().GetByIdWithRoleAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDashboardAsync_ContainersByRisk_FillsMissingLevelsWithZero()
    {
        var jefe = BuildJefe(2);
        var analista = new Usuario { Id = 1, NombreUsuario = "Camila", JefeId = 2 };

        var containers = new List<Container>
        {
            new() { Id = 1, NumeroContainer = "C1", Vigente = true, AsignadoA = 1, Asignado = analista, Riesgo = new Riesgo { Id = 3, Descripcion = "CRITICAL" } },
            new() { Id = 2, NumeroContainer = "C2", Vigente = true, AsignadoA = 1, Asignado = analista, Riesgo = new Riesgo { Id = 3, Descripcion = "CRITICAL" } },
            new() { Id = 3, NumeroContainer = "C3", Vigente = true, AsignadoA = 1, Asignado = analista, Riesgo = null },
        };
        var containersMock = containers.BuildMockDbSet();
        _containerRepository.Query().Returns(containersMock);
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(jefe);
        _usuarioRepository.GetPagedAsync(null, null, 2, null, 1, 1000, Arg.Any<CancellationToken>())
            .Returns((new List<Usuario> { analista }, 1L));

        var result = await _sut.GetDashboardAsync(2, callerUserId: 2, callerRole: "Jefe", new TeamDashboardFilter(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var byRisk = result.Value.ContainersByRisk.ToDictionary(x => x.RiskLevel, x => x.Count);
        Assert.Equal(2, byRisk["CRITICAL"]);
        Assert.Equal(1, byRisk["OK"]);
        Assert.Equal(0, byRisk["WARNING"]);
        Assert.Equal(0, byRisk["OVERDUE"]);
        Assert.Equal(4, result.Value.ContainersByRisk.Count);
    }

    [Fact]
    public async Task GetDashboardAsync_AlertMetrics_ComputesGeneratedManagedAndAvgReaction()
    {
        var jefe = BuildJefe(2);
        var analista = new Usuario { Id = 1, NombreUsuario = "Camila", JefeId = 2 };
        var otroAnalista = new Usuario { Id = 5, NombreUsuario = "Diego", JefeId = 2 };
        var container = new Container { Id = 1, NumeroContainer = "C1", AsignadoA = 1, Asignado = analista };

        var ahora = DateTimeOffset.UtcNow;
        var alertas = new List<Alerta>
        {
            new() { Id = 1, IdContainer = 1, Container = container, Riesgo = new Riesgo { Descripcion = "CRITICAL" },
                    TipoAlerta = new TipoAlerta { Descripcion = "X" }, Leida = true,
                    CreateAt = ahora.AddHours(-2), FechaLectura = ahora.AddHours(-1) }, // 60 min de reacción
            new() { Id = 2, IdContainer = 1, Container = container, Riesgo = new Riesgo { Descripcion = "CRITICAL" },
                    TipoAlerta = new TipoAlerta { Descripcion = "X" }, Leida = true,
                    CreateAt = ahora.AddHours(-4), FechaLectura = ahora.AddHours(-2) }, // 120 min de reacción
            new() { Id = 3, IdContainer = 1, Container = container, Riesgo = new Riesgo { Descripcion = "WARNING" },
                    TipoAlerta = new TipoAlerta { Descripcion = "X" }, Leida = false, CreateAt = ahora },
        };
        var alertasMock = alertas.BuildMockDbSet();
        _alertaRepository.Query().Returns(alertasMock);
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(jefe);
        _usuarioRepository.GetPagedAsync(null, null, 2, null, 1, 1000, Arg.Any<CancellationToken>())
            .Returns((new List<Usuario> { analista, otroAnalista }, 2L));

        var result = await _sut.GetDashboardAsync(2, callerUserId: 2, callerRole: "Jefe", new TeamDashboardFilter(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var camila = result.Value.AlertsByAnalyst.Single(a => a.AnalystId == 1);
        Assert.Equal(3, camila.Generated);
        Assert.Equal(2, camila.Managed);

        var diego = result.Value.AlertsByAnalyst.Single(a => a.AnalystId == 5);
        Assert.Equal(0, diego.Generated);
        Assert.Equal(0, diego.Managed);

        Assert.NotNull(result.Value.AvgReactionTimeCriticalMinutes);
        Assert.Equal(90, result.Value.AvgReactionTimeCriticalMinutes!.Value, precision: 3);
    }

    [Fact]
    public async Task GetDashboardAsync_WithNoCriticalManagedAlerts_ReturnsNullAvgReaction()
    {
        var jefe = BuildJefe(2);
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(jefe);
        _usuarioRepository.GetPagedAsync(null, null, 2, null, 1, 1000, Arg.Any<CancellationToken>())
            .Returns((new List<Usuario>(), 0L));

        var result = await _sut.GetDashboardAsync(2, callerUserId: 2, callerRole: "Jefe", new TeamDashboardFilter(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.AvgReactionTimeCriticalMinutes);
    }

    [Fact]
    public async Task GetDashboardAsync_DocumentsByPeriod_ZeroFillsMissingDays()
    {
        var jefe = BuildJefe(2);
        var analista = new Usuario { Id = 1, NombreUsuario = "Camila", JefeId = 2 };
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(jefe);
        _usuarioRepository.GetPagedAsync(null, null, 2, null, 1, 1000, Arg.Any<CancellationToken>())
            .Returns((new List<Usuario> { analista }, 1L));

        var hoy = DateTimeOffset.UtcNow.Date;
        var cargas = new List<CargaDocumentos>
        {
            new() { Id = 1, UserId = 1, Usuario = analista, Vigente = true, CreacionFecha = hoy },
            new() { Id = 2, UserId = 1, Usuario = analista, Vigente = true, CreacionFecha = hoy },
        };
        var cargasMock = cargas.BuildMockDbSet();
        _cargaDocumentosRepository.Query().Returns(cargasMock);

        var filter = new TeamDashboardFilter { FromDate = hoy.AddDays(-2), ToDate = hoy };
        var result = await _sut.GetDashboardAsync(2, callerUserId: 2, callerRole: "Jefe", filter, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.DocumentsByPeriod.Count); // 3 días en el rango
        Assert.Equal(2, result.Value.DocumentsByPeriod.Last().Count);
        Assert.Equal(0, result.Value.DocumentsByPeriod.First().Count);
    }
}
