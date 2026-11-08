namespace PortIA.Core.Tests.Component.Mensajeria;

using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Mensajeria.Configuration;
using PortIA.Core.Services.Component.Mensajeria.Consumers;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Riesgo.Interfaces;

public class RealStatusUpdatedConsumerTests
{
    private readonly IContainerRepository _containerRepository;
    private readonly IClimaRepository _climaRepository;
    private readonly IAccionRepository _accionRepository;
    private readonly ILogAuditoriaRepository _logAuditoriaRepository;
    private readonly IEstadoContainerRepository _estadoContainerRepository;
    private readonly IHitoRepository _hitoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRiskEngineService _riskEngineService;
    private readonly RealStatusUpdatedConsumer _sut;

    public RealStatusUpdatedConsumerTests()
    {
        _containerRepository = Substitute.For<IContainerRepository>();
        _climaRepository = Substitute.For<IClimaRepository>();
        _accionRepository = Substitute.For<IAccionRepository>();
        _logAuditoriaRepository = Substitute.For<ILogAuditoriaRepository>();
        _estadoContainerRepository = Substitute.For<IEstadoContainerRepository>();
        _hitoRepository = Substitute.For<IHitoRepository>();
        _riskEngineService = Substitute.For<IRiskEngineService>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Containers.Returns(_containerRepository);
        _unitOfWork.Climas.Returns(_climaRepository);
        _unitOfWork.Acciones.Returns(_accionRepository);
        _unitOfWork.LogsAuditoria.Returns(_logAuditoriaRepository);
        _unitOfWork.EstadosContainer.Returns(_estadoContainerRepository);
        _unitOfWork.Hitos.Returns(_hitoRepository);

        _climaRepository.GetByDescripcionAsync("Sin alerta", Arg.Any<CancellationToken>())
            .Returns(new Clima { Id = 1, Descripcion = "Sin alerta" });
        _climaRepository.GetByDescripcionAsync("Marejada", Arg.Any<CancellationToken>())
            .Returns(new Clima { Id = 2, Descripcion = "Marejada" });
        _accionRepository.GetByDescripcionAsync("ACTUALIZAR", Arg.Any<CancellationToken>())
            .Returns(new Accion { Id = 2, Descripcion = "ACTUALIZAR" });
        _estadoContainerRepository.GetByDescripcionAsync("RETIRADO", Arg.Any<CancellationToken>())
            .Returns(new EstadoContainer { Id = 10, Descripcion = "RETIRADO" });
        _estadoContainerRepository.GetByDescripcionAsync("DEVUELTO_VACIO", Arg.Any<CancellationToken>())
            .Returns(new EstadoContainer { Id = 12, Descripcion = "DEVUELTO_VACIO" });

        var discrepanciaEtaOptions = Options.Create(new DiscrepanciaEtaOptions { UmbralHoras = 48 });

        _sut = new RealStatusUpdatedConsumer(
            _unitOfWork, _riskEngineService, discrepanciaEtaOptions, Substitute.For<ILogger<RealStatusUpdatedConsumer>>());
    }

    private static ConsumeContext<RealStatusUpdated> BuildContext(RealStatusUpdated evento)
    {
        var context = Substitute.For<ConsumeContext<RealStatusUpdated>>();
        context.Message.Returns(evento);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_WhenContainerDoesNotExist_DoesNothing()
    {
        _containerRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Container?)null);
        var evento = new RealStatusUpdated(99, "MSCU1234567", DateTimeOffset.UtcNow, "En patio", false, null, "MOCK", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_FirstInspection_SetsClimaFromWeatherAlertAndDoesNotAudit()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", DiasLibresDemurrage = 7, FechaArriboReal = null };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var evento = new RealStatusUpdated(1, "MSCU1234567", DateTimeOffset.UtcNow, "En patio", true, null, "SCRAPING", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        Assert.Equal(2, container.IdClima); // 'Marejada'
        await _logAuditoriaRepository.DidNotReceive().AddAsync(Arg.Any<LogAuditoria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithoutWeatherAlert_SetsClimaSinAlerta()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", DiasLibresDemurrage = 7, FechaArriboReal = null };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var evento = new RealStatusUpdated(1, "MSCU1234567", DateTimeOffset.UtcNow, "En patio", false, null, "SCRAPING", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        Assert.Equal(1, container.IdClima); // 'Sin alerta'
    }

    [Fact]
    public async Task Consume_WithRealScrapingSource_RecalculatesRisk()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", DiasLibresDemurrage = 7, FechaArriboReal = null };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var evento = new RealStatusUpdated(1, "MSCU1234567", DateTimeOffset.UtcNow, "En patio", false, null, "SCRAPING", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        await _riskEngineService.Received(1).RecalcularAsync(container, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_Reinspection_RegistersAuditWithPreviousAndNewValues()
    {
        var fechaPrevia = DateTimeOffset.UtcNow.AddDays(-1);
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            DiasLibresDemurrage = 7,
            FechaArriboReal = fechaPrevia,
            EstadoPuerto = "En patio",
            IdClima = 1,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var nuevaFecha = DateTimeOffset.UtcNow;
        var evento = new RealStatusUpdated(1, "MSCU1234567", nuevaFecha, "Descargado", false, null, "SCRAPING", nuevaFecha);

        await _sut.Consume(BuildContext(evento));

        await _logAuditoriaRepository.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(log =>
                log.NombreEntidad == nameof(Container) &&
                log.IdEntidad == 1 &&
                log.IdAccion == 2 &&
                log.ValoresAnteriores!.Contains("En patio") &&
                log.ValoresNuevos!.Contains("Descargado")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithMockScraperSource_DiscardsPortStatusAndDoesNotRecalculateRisk()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            DiasLibresDemurrage = 7,
            FechaArriboReal = null,
            EstadoPuerto = null,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var fechaSintetica = DateTimeOffset.UtcNow.AddHours(-30);
        var evento = new RealStatusUpdated(1, "MSCU1234567", fechaSintetica, "En patio", false, null, "MOCK", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        Assert.Null(container.FechaArriboReal);
        Assert.Null(container.EstadoPuerto);
        Assert.Null(container.DeadlineDemurrage);
        await _riskEngineService.DidNotReceive().RecalcularAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithEstadoContenedorRetirado_SetsFechaRetiroAndRegistersScraperHito()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            DiasLibresDemurrage = 7,
            DiasLibresDetention = 5,
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-3),
            FechaRetiroContainer = null,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var evento = new RealStatusUpdated(1, "MSCU1234567", container.FechaArriboReal, "Retirado", false, null, "SCRAPING", DateTimeOffset.UtcNow, "RETIRADO");

        await _sut.Consume(BuildContext(evento));

        Assert.NotNull(container.FechaRetiroContainer);
        Assert.Equal(10, container.IdEstado);
        Assert.NotNull(container.DeadlineDetention);
        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 1 && h.Origen == "SCRAPER" && h.CreadoPor == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithEstadoContenedorRetirado_WhenAlreadyRetired_DoesNotOverwrite()
    {
        var fechaRetiroOriginal = DateTimeOffset.UtcNow.AddDays(-1);
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-3),
            FechaRetiroContainer = fechaRetiroOriginal,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var evento = new RealStatusUpdated(1, "MSCU1234567", container.FechaArriboReal, "Retirado", false, null, "SCRAPING", DateTimeOffset.UtcNow, "RETIRADO");

        await _sut.Consume(BuildContext(evento));

        Assert.Equal(fechaRetiroOriginal, container.FechaRetiroContainer);
        await _hitoRepository.DidNotReceive().AddAsync(Arg.Any<Hito>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithEstadoContenedorDevueltoVacio_WhenRetiredBefore_SetsEstadoAndRegistersScraperHito()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-5),
            FechaRetiroContainer = DateTimeOffset.UtcNow.AddDays(-1),
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var evento = new RealStatusUpdated(1, "MSCU1234567", container.FechaArriboReal, "Devuelto", false, null, "SCRAPING", DateTimeOffset.UtcNow, "DEVUELTO_VACIO");

        await _sut.Consume(BuildContext(evento));

        Assert.Equal(12, container.IdEstado);
        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 1 && h.Origen == "SCRAPER"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithUnrecognizedEstadoContenedor_DoesNotMutateContainerState()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-3),
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var evento = new RealStatusUpdated(1, "MSCU1234567", container.FechaArriboReal, "En patio", false, null, "SCRAPING", DateTimeOffset.UtcNow, EstadoContenedor: null);

        await _sut.Consume(BuildContext(evento));

        Assert.Null(container.FechaRetiroContainer);
        Assert.Null(container.IdEstado);
        await _hitoRepository.DidNotReceive().AddAsync(Arg.Any<Hito>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithoutFechaLlegadaEstimada_DoesNotCheckDiscrepancy()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaLlegadaEstimada = null,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var arribo = DateTimeOffset.UtcNow;
        var evento = new RealStatusUpdated(1, "MSCU1234567", arribo, "En patio", false, null, "SCRAPING", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        await _hitoRepository.DidNotReceive().AddAsync(Arg.Any<Hito>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithArrivalWithinThreshold_DoesNotRegisterDiscrepancyHito()
    {
        var eta = DateTimeOffset.UtcNow;
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaLlegadaEstimada = eta,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var arribo = eta.AddHours(24);
        var evento = new RealStatusUpdated(1, "MSCU1234567", arribo, "En patio", false, null, "SCRAPING", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        await _hitoRepository.DidNotReceive().AddAsync(Arg.Any<Hito>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithArrivalMuchLaterThanEta_RegistersDiscrepancyHito()
    {
        var eta = DateTimeOffset.UtcNow;
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaLlegadaEstimada = eta,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var arribo = eta.AddHours(72);
        var evento = new RealStatusUpdated(1, "MSCU1234567", arribo, "En patio", false, null, "SCRAPING", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 1 && h.Origen == "SCRAPER" && h.Descripcion.Contains("más tarde")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithArrivalMuchEarlierThanEta_RegistersDiscrepancyHito()
    {
        var eta = DateTimeOffset.UtcNow;
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaLlegadaEstimada = eta,
        };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var arribo = eta.AddHours(-72);
        var evento = new RealStatusUpdated(1, "MSCU1234567", arribo, "En patio", false, null, "SCRAPING", DateTimeOffset.UtcNow);

        await _sut.Consume(BuildContext(evento));

        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 1 && h.Origen == "SCRAPER" && h.Descripcion.Contains("más temprano")),
            Arg.Any<CancellationToken>());
    }
}
