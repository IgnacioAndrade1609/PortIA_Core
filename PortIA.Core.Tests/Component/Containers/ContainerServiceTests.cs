namespace PortIA.Core.Tests.Component.Containers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Containers.DTOs;
using PortIA.Core.Services.Component.Containers.Services;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;
using PortIA.Core.Services.Component.Riesgo.Configuration;
using PortIA.Core.Services.Component.Riesgo.Interfaces;

public class ContainerServiceTests
{
    private readonly IContainerRepository _containerRepository;
    private readonly IEstadoContainerRepository _estadoContainerRepository;
    private readonly IHitoRepository _hitoRepository;
    private readonly IAccionRepository _accionRepository;
    private readonly ILogAuditoriaRepository _logAuditoriaRepository;
    private readonly ITerminalRepository _terminalRepository;
    private readonly INavieraRepository _navieraRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInspectContainerPublisher _inspectContainerPublisher;
    private readonly IRiskEngineService _riskEngineService;
    private readonly ContainerService _sut;

    public ContainerServiceTests()
    {
        _containerRepository = Substitute.For<IContainerRepository>();
        _estadoContainerRepository = Substitute.For<IEstadoContainerRepository>();
        _hitoRepository = Substitute.For<IHitoRepository>();
        _accionRepository = Substitute.For<IAccionRepository>();
        _logAuditoriaRepository = Substitute.For<ILogAuditoriaRepository>();
        _terminalRepository = Substitute.For<ITerminalRepository>();
        _navieraRepository = Substitute.For<INavieraRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Containers.Returns(_containerRepository);
        _unitOfWork.EstadosContainer.Returns(_estadoContainerRepository);
        _unitOfWork.Hitos.Returns(_hitoRepository);
        _unitOfWork.Acciones.Returns(_accionRepository);
        _unitOfWork.LogsAuditoria.Returns(_logAuditoriaRepository);
        _unitOfWork.Terminales.Returns(_terminalRepository);
        _unitOfWork.Navieras.Returns(_navieraRepository);

        _accionRepository.GetByDescripcionAsync("ACTUALIZAR", Arg.Any<CancellationToken>())
            .Returns(new Accion { Id = 2, Descripcion = "ACTUALIZAR" });

        var terminales = new List<Terminal>
        {
            new() { Id = 1, CodigoTerminal = "STI", NombreTerminal = "San Antonio Terminal Internacional" },
            new() { Id = 2, CodigoTerminal = "TPS", NombreTerminal = "Terminal Pacífico Sur Valparaíso" },
        };
        var terminalesMock = terminales.BuildMockDbSet();
        _terminalRepository.Query().Returns(terminalesMock);

        var navieras = new List<Naviera>
        {
            new() { Id = 1, NombreNaviera = "MSC" },
            new() { Id = 2, NombreNaviera = "Maersk" },
        };
        var navierasMock = navieras.BuildMockDbSet();
        _navieraRepository.Query().Returns(navierasMock);

        _inspectContainerPublisher = Substitute.For<IInspectContainerPublisher>();
        _riskEngineService = Substitute.For<IRiskEngineService>();

        var riskOptions = Options.Create(new RiskEngineOptions
        {
            HorasUmbralOk = 96,
            HorasUmbralWarning = 48,
            HorasUmbralCritical = 0,
        });

        _sut = new ContainerService(_unitOfWork, _inspectContainerPublisher, _riskEngineService, riskOptions, Substitute.For<ILogger<ContainerService>>());
    }

    [Fact]
    public async Task TriggerInspectionAsync_DelegatesToPublisher()
    {
        _inspectContainerPublisher.PublishAsync(5, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _sut.TriggerInspectionAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _inspectContainerPublisher.Received(1).PublishAsync(5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHitosAsync_WhenContainerDoesNotExist_ReturnsNotFound()
    {
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Container?)null);

        var result = await _sut.GetHitosAsync(1, userId: 7, userRole: "Analista", CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetHitosAsync_WhenContainerBelongsToAnotherAnalista_ReturnsNotFound()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.GetHitosAsync(1, userId: 99, userRole: "Analista", CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetHitosAsync_AsAdmin_CanViewAnyContainer()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);
        var mockContainers = new List<Container> { container }.BuildMockDbSet();
        _containerRepository.Query().Returns(mockContainers);

        var result = await _sut.GetHitosAsync(1, userId: 99, userRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetHitosAsync_AsJefeOfTheAssignedAnalista_CanViewContainer()
    {
        var analista = new Usuario { Id = 7, NombreUsuario = "Camila", JefeId = 2 };
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7, Asignado = analista };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);
        var mockContainers = new List<Container> { container }.BuildMockDbSet();
        _containerRepository.Query().Returns(mockContainers);

        var result = await _sut.GetHitosAsync(1, userId: 2, userRole: "Jefe", CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetHitosAsync_AsJefeOfAnotherTeam_ReturnsNotFound()
    {
        var analista = new Usuario { Id = 7, NombreUsuario = "Camila", JefeId = 2 };
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7, Asignado = analista };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.GetHitosAsync(1, userId: 99, userRole: "Jefe", CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetHitosAsync_WhenContainerBelongsToUser_ReturnsHitosOrderedByDate()
    {
        var now = DateTimeOffset.UtcNow;
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            Hitos = new List<Hito>
            {
                new() { Id = 2, IdContainer = 1, Descripcion = "Segundo", Origen = "IA", FechaOcurrencia = now },
                new() { Id = 1, IdContainer = 1, Descripcion = "Primero", Origen = "IA", FechaOcurrencia = now.AddHours(-1) },
            }
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var mockContainers = new List<Container> { container }.BuildMockDbSet();
        _containerRepository.Query().Returns(mockContainers);

        var result = await _sut.GetHitosAsync(1, userId: 7, userRole: "Analista", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("Primero", result.Value[0].Descripcion);
        Assert.Equal("Segundo", result.Value[1].Descripcion);
    }

    [Fact]
    public async Task MarkRetiroAsync_WhenContainerBelongsToAnotherUser_ReturnsNotFound()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.MarkRetiroAsync(1, userId: 99, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task MarkRetiroAsync_WhenAlreadyRetired_ReturnsBusinessError()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            FechaRetiroContainer = DateTimeOffset.UtcNow.AddDays(-1),
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.MarkRetiroAsync(1, userId: 7, CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
    }

    [Fact]
    public async Task MarkRetiroAsync_WhenArrivalNotConfirmedYet_ReturnsBusinessError()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            FechaArriboReal = null,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.MarkRetiroAsync(1, userId: 7, CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _riskEngineService.DidNotReceive().RecalcularAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkRetiroAsync_WhenValid_SetsFechaRetiroEstadoAndRecalculatesRisk()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            DiasLibresDetention = 5,
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-2),
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);
        _estadoContainerRepository.GetByDescripcionAsync("RETIRADO", Arg.Any<CancellationToken>())
            .Returns(new EstadoContainer { Id = 10, Descripcion = "RETIRADO" });

        var result = await _sut.MarkRetiroAsync(1, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(container.FechaRetiroContainer);
        Assert.Equal(10, container.IdEstado);
        Assert.NotNull(container.DeadlineDetention);
        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 1 && h.Origen == "USUARIO" && h.CreadoPor == 7),
            Arg.Any<CancellationToken>());
        await _riskEngineService.Received(1).RecalcularAsync(container, Arg.Any<CancellationToken>());
        await _logAuditoriaRepository.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(l => l.NombreEntidad == nameof(Container) && l.IdEntidad == 1 && l.Responsable == 7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkDevolucionVaciaAsync_WhenNotRetiredYet_ReturnsBusinessError()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7, FechaRetiroContainer = null };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.MarkDevolucionVaciaAsync(1, userId: 7, CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
    }

    [Fact]
    public async Task MarkDevolucionVaciaAsync_WhenValid_SetsEstadoAndRecalculatesRisk()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            FechaRetiroContainer = DateTimeOffset.UtcNow.AddDays(-2),
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);
        _estadoContainerRepository.GetByDescripcionAsync("DEVUELTO_VACIO", Arg.Any<CancellationToken>())
            .Returns(new EstadoContainer { Id = 12, Descripcion = "DEVUELTO_VACIO" });

        var result = await _sut.MarkDevolucionVaciaAsync(1, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, container.IdEstado);
        await _riskEngineService.Received(1).RecalcularAsync(container, Arg.Any<CancellationToken>());
        await _logAuditoriaRepository.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(l => l.NombreEntidad == nameof(Container) && l.IdEntidad == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkDevolucionVaciaAsync_WhenAlreadyReturned_ReturnsBusinessError()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            FechaRetiroContainer = DateTimeOffset.UtcNow.AddDays(-2),
            EstadoContainer = new EstadoContainer { Id = 12, Descripcion = "DEVUELTO_VACIO" },
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.MarkDevolucionVaciaAsync(1, userId: 7, CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _riskEngineService.DidNotReceive().RecalcularAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmarArriboRealAsync_WhenContainerBelongsToAnotherUser_ReturnsNotFound()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.ConfirmarArriboRealAsync(1, userId: 99, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ConfirmarArriboRealAsync_WithFutureDate_ReturnsBusinessError()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.ConfirmarArriboRealAsync(1, userId: 7, DateTimeOffset.UtcNow.AddDays(1), CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _riskEngineService.DidNotReceive().RecalcularAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmarArriboRealAsync_WhenValid_SetsFechaArriboRealDeadlineAndRecalculatesRisk()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            DiasLibresDemurrage = 7,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var fechaArribo = DateTimeOffset.UtcNow.AddDays(-2);
        var result = await _sut.ConfirmarArriboRealAsync(1, userId: 7, fechaArribo, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fechaArribo, container.FechaArriboReal);
        Assert.Equal(fechaArribo.AddDays(7), container.DeadlineDemurrage);
        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 1 && h.Origen == "USUARIO" && h.CreadoPor == 7),
            Arg.Any<CancellationToken>());
        await _riskEngineService.Received(1).RecalcularAsync(container, Arg.Any<CancellationToken>());
        await _logAuditoriaRepository.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(l => l.NombreEntidad == nameof(Container) && l.IdEntidad == 1 && l.Responsable == 7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDiasLibresAsync_WhenContainerBelongsToAnotherUser_ReturnsNotFound()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.UpdateDiasLibresAsync(1, userId: 99, 5, 7, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(5, -1)]
    public async Task UpdateDiasLibresAsync_WithNegativeValues_ReturnsBusinessError(int demurrage, int detention)
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.UpdateDiasLibresAsync(1, userId: 7, demurrage, detention, CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _riskEngineService.DidNotReceive().RecalcularAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDiasLibresAsync_WhenNoAnchorDatesYet_UpdatesFreeDaysWithoutTouchingDeadlines()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            DiasLibresDemurrage = 5,
            DiasLibresDetention = 5,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.UpdateDiasLibresAsync(1, userId: 7, 10, 8, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, container.DiasLibresDemurrage);
        Assert.Equal(8, container.DiasLibresDetention);
        Assert.Null(container.DeadlineDemurrage);
        Assert.Null(container.DeadlineDetention);
        await _riskEngineService.Received(1).RecalcularAsync(container, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDiasLibresAsync_WhenArrivedAndRetired_RecalculatesBothDeadlines()
    {
        var fechaArribo = DateTimeOffset.UtcNow.AddDays(-3);
        var fechaRetiro = DateTimeOffset.UtcNow.AddDays(-1);
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            FechaArriboReal = fechaArribo,
            FechaRetiroContainer = fechaRetiro,
            DiasLibresDemurrage = 5,
            DiasLibresDetention = 5,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.UpdateDiasLibresAsync(1, userId: 7, 10, 8, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fechaArribo.AddDays(10), container.DeadlineDemurrage);
        Assert.Equal(fechaRetiro.AddDays(8), container.DeadlineDetention);
        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 1 && h.Origen == "USUARIO" && h.CreadoPor == 7),
            Arg.Any<CancellationToken>());
        await _riskEngineService.Received(1).RecalcularAsync(container, Arg.Any<CancellationToken>());
        await _logAuditoriaRepository.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(l => l.NombreEntidad == nameof(Container) && l.IdEntidad == 1 && l.Responsable == 7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDatosExtraidosAsync_WhenContainerBelongsToAnotherUser_ReturnsNotFound()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.UpdateDatosExtraidosAsync(1, userId: 99, new UpdateContainerMetadataRequest(null, null, null, null, null, null), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateDatosExtraidosAsync_WhenNavieraDoesNotExistInCatalog_ReturnsBusinessError()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var request = new UpdateContainerMetadataRequest("Naviera Inexistente", null, null, null, null, null);
        var result = await _sut.UpdateDatosExtraidosAsync(1, userId: 7, request, CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _hitoRepository.DidNotReceive().AddAsync(Arg.Any<Hito>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDatosExtraidosAsync_WhenNothingActuallyChanges_DoesNotWriteHitoOrAudit()
    {
        var naviera = new Naviera { Id = 1, NombreNaviera = "MSC" };
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7, IdNaviera = 1, Naviera = naviera };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var request = new UpdateContainerMetadataRequest("MSC", null, null, null, null, null);
        var result = await _sut.UpdateDatosExtraidosAsync(1, userId: 7, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _hitoRepository.DidNotReceive().AddAsync(Arg.Any<Hito>(), Arg.Any<CancellationToken>());
        await _logAuditoriaRepository.DidNotReceive().AddAsync(Arg.Any<LogAuditoria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDatosExtraidosAsync_WhenValid_UpdatesFieldsAndWritesHitoAndAudit()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            AsignadoA = 7,
            NumeroBl = "BL-VIEJO",
            NombreForwarder = "Forwarder Viejo",
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var nuevaEta = DateTimeOffset.UtcNow.AddDays(5);
        var request = new UpdateContainerMetadataRequest("Maersk", "San Antonio Terminal Internacional", "Terminal Pacífico Sur Valparaíso", "BL-NUEVO", "Forwarder Nuevo", nuevaEta);
        var result = await _sut.UpdateDatosExtraidosAsync(1, userId: 7, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, container.IdNaviera);
        Assert.Equal(1, container.IdTerminalOrigen);
        Assert.Equal(2, container.IdTerminalLlegada);
        Assert.Equal("BL-NUEVO", container.NumeroBl);
        Assert.Equal("Forwarder Nuevo", container.NombreForwarder);
        Assert.Equal(nuevaEta, container.FechaLlegadaEstimada);
        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 1 && h.Origen == "USUARIO" && h.CreadoPor == 7),
            Arg.Any<CancellationToken>());
        await _logAuditoriaRepository.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(l => l.NombreEntidad == nameof(Container) && l.IdEntidad == 1 && l.Responsable == 7),
            Arg.Any<CancellationToken>());
        await _riskEngineService.DidNotReceive().RecalcularAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    private static List<Container> BuildEquipoContainers()
    {
        var jefe1 = new Usuario { Id = 2, NombreUsuario = "Felipe Soto", JefeId = null };
        var analista1DeJefe1 = new Usuario { Id = 1, NombreUsuario = "Camila Rojas", JefeId = 2 };
        var jefe2 = new Usuario { Id = 7, NombreUsuario = "Pedro", JefeId = null };
        var analista1DeJefe2 = new Usuario { Id = 6, NombreUsuario = "test", JefeId = 7 };

        return new List<Container>
        {
            new() { Id = 1, NumeroContainer = "C1", AsignadoA = 1, Asignado = analista1DeJefe1, Vigente = true },
            new() { Id = 2, NumeroContainer = "C2", AsignadoA = 6, Asignado = analista1DeJefe2, Vigente = true },
            new() { Id = 3, NumeroContainer = "C3", AsignadoA = 2, Asignado = jefe1, Vigente = true },
        };
    }

    [Fact]
    public async Task GetContainersAsync_AsJefe_OnlyReturnsOwnTeamContainers()
    {
        var mockSet = BuildEquipoContainers().BuildMockDbSet();
        _containerRepository.Query().Returns(mockSet);

        var result = await _sut.GetContainersAsync(
            userId: 2, userRole: "Jefe",
            search: null, riskLevel: null, shippingLineId: null, originTerminalId: null, destinationTerminalId: null,
            assignedTo: null, jefeId: null, managed: null, needsReview: null, fromDate: null, toDate: null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("C1", result.Value[0].ContainerNumber);
    }

    [Fact]
    public async Task GetContainersAsync_AsJefe_IgnoresRequestedJefeIdFromAnotherJefe()
    {
        var mockSet = BuildEquipoContainers().BuildMockDbSet();
        _containerRepository.Query().Returns(mockSet);

        var result = await _sut.GetContainersAsync(
            userId: 2, userRole: "Jefe",
            search: null, riskLevel: null, shippingLineId: null, originTerminalId: null, destinationTerminalId: null,
            assignedTo: null, jefeId: 7, managed: null, needsReview: null, fromDate: null, toDate: null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("C1", result.Value[0].ContainerNumber);
    }

    [Fact]
    public async Task GetContainersAsync_AsAdmin_WithoutJefeId_ReturnsAllContainers()
    {
        var mockSet = BuildEquipoContainers().BuildMockDbSet();
        _containerRepository.Query().Returns(mockSet);

        var result = await _sut.GetContainersAsync(
            userId: 99, userRole: "Admin",
            search: null, riskLevel: null, shippingLineId: null, originTerminalId: null, destinationTerminalId: null,
            assignedTo: null, jefeId: null, managed: null, needsReview: null, fromDate: null, toDate: null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public async Task GetContainersAsync_AsAdmin_WithJefeId_FiltersByThatTeam()
    {
        var mockSet = BuildEquipoContainers().BuildMockDbSet();
        _containerRepository.Query().Returns(mockSet);

        var result = await _sut.GetContainersAsync(
            userId: 99, userRole: "Admin",
            search: null, riskLevel: null, shippingLineId: null, originTerminalId: null, destinationTerminalId: null,
            assignedTo: null, jefeId: 7, managed: null, needsReview: null, fromDate: null, toDate: null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("C2", result.Value[0].ContainerNumber);
    }
}
