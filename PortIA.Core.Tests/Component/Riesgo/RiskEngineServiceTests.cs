namespace PortIA.Core.Tests.Component.Riesgo;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Alerts.DTOs;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.Services.Component.Riesgo.Configuration;
using PortIA.Core.Services.Component.Riesgo.Services;

public class RiskEngineServiceTests
{
    private readonly IRiesgoRepository _riesgoRepository;
    private readonly ITipoAlertaRepository _tipoAlertaRepository;
    private readonly IAlertaRepository _alertaRepository;
    private readonly IContainerRepository _containerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAlertNotifier _alertNotifier;
    private readonly RiskEngineService _sut;

    public RiskEngineServiceTests()
    {
        _riesgoRepository = Substitute.For<IRiesgoRepository>();
        _tipoAlertaRepository = Substitute.For<ITipoAlertaRepository>();
        _alertaRepository = Substitute.For<IAlertaRepository>();
        _containerRepository = Substitute.For<IContainerRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Riesgos.Returns(_riesgoRepository);
        _unitOfWork.TiposAlerta.Returns(_tipoAlertaRepository);
        _unitOfWork.Alertas.Returns(_alertaRepository);
        _unitOfWork.Containers.Returns(_containerRepository);

        foreach (var (id, descripcion) in new[] { (1, "OK"), (2, "WARNING"), (3, "CRITICAL"), (4, "OVERDUE") })
        {
            _riesgoRepository.GetByDescripcionAsync(descripcion, Arg.Any<CancellationToken>())
                .Returns(new Riesgo { Id = id, Descripcion = descripcion });
        }

        _tipoAlertaRepository.GetByDescripcionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult<TipoAlerta?>(new TipoAlerta { Id = 1, Descripcion = callInfo.ArgAt<string>(0) }));

        var options = Options.Create(new RiskEngineOptions
        {
            HorasUmbralOk = 96,
            HorasUmbralWarning = 48,
            HorasUmbralCritical = 0,
        });

        _alertNotifier = Substitute.For<IAlertNotifier>();

        _sut = new RiskEngineService(_unitOfWork, options, _alertNotifier, Substitute.For<ILogger<RiskEngineService>>());
    }

    [Fact]
    public async Task RecalcularAsync_WhenContainerNotArrivedYet_ReturnsOkBecauseClockHasNotStarted()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = null,
            DiasLibresDemurrage = 7,
        };

        await _sut.RecalcularAsync(container, CancellationToken.None);

        Assert.Equal(1, container.IdRiesgo);
        await _alertaRepository.DidNotReceive().AddAsync(Arg.Any<Alerta>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(100, "OK", 1)]
    [InlineData(72, "WARNING", 2)]
    [InlineData(24, "CRITICAL", 3)]
    [InlineData(-10, "OVERDUE", 4)]
    public async Task RecalcularAsync_WhenArrivedAndDemurrageActive_ClassifiesByHoursToDeadline(
        double horasHastaDeadline, string nivelEsperado, int idRiesgoEsperado)
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-1),
            DeadlineDemurrage = DateTimeOffset.UtcNow.AddHours(horasHastaDeadline),
            DiasLibresDemurrage = 7,
        };

        await _sut.RecalcularAsync(container, CancellationToken.None);

        Assert.Equal(idRiesgoEsperado, container.IdRiesgo);

        if (nivelEsperado == "OK")
        {
            await _alertaRepository.DidNotReceive().AddAsync(Arg.Any<Alerta>(), Arg.Any<CancellationToken>());
        }
        else
        {
            await _alertaRepository.Received(1).AddAsync(
                Arg.Is<Alerta>(a => a.IdContainer == 1 && a.IdRiesgo == idRiesgoEsperado),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task RecalcularAsync_WhenRetired_UsesDetentionDeadlineInsteadOfDemurrage()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-5),
            DeadlineDemurrage = DateTimeOffset.UtcNow.AddHours(-100),
            FechaRetiroContainer = DateTimeOffset.UtcNow.AddDays(-1),
            DeadlineDetention = DateTimeOffset.UtcNow.AddHours(72),
            DiasLibresDetention = 5,
        };

        await _sut.RecalcularAsync(container, CancellationToken.None);

        Assert.Equal(2, container.IdRiesgo);
        await _alertaRepository.Received(1).AddAsync(
            Arg.Is<Alerta>(a => a.Mensaje.Contains("DETENTION")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecalcularAsync_WhenReturnedEmpty_ReturnsOkRegardlessOfDeadlines()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-10),
            FechaRetiroContainer = DateTimeOffset.UtcNow.AddDays(-3),
            DeadlineDetention = DateTimeOffset.UtcNow.AddHours(-200),
            EstadoContainer = new EstadoContainer { Id = 12, Descripcion = "DEVUELTO_VACIO" },
        };

        await _sut.RecalcularAsync(container, CancellationToken.None);

        Assert.Equal(1, container.IdRiesgo);
        await _alertaRepository.DidNotReceive().AddAsync(Arg.Any<Alerta>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecalcularAsync_WhenSeverityDoesNotEscalate_DoesNotCreateDuplicateAlert()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-1),
            DeadlineDemurrage = DateTimeOffset.UtcNow.AddHours(24),
            Riesgo = new Riesgo { Id = 3, Descripcion = "CRITICAL" },
        };

        await _sut.RecalcularAsync(container, CancellationToken.None);

        Assert.Equal(3, container.IdRiesgo);
        await _alertaRepository.DidNotReceive().AddAsync(Arg.Any<Alerta>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecalcularAsync_WhenAlertCreatedAndContainerIsAssigned_NotifiesAssignedAnalyst()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-1),
            DeadlineDemurrage = DateTimeOffset.UtcNow.AddHours(24),
            AsignadoA = 7,
        };
        _alertaRepository.AddAsync(Arg.Any<Alerta>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => callInfo.Arg<Alerta>().Id = 55);

        await _sut.RecalcularAsync(container, CancellationToken.None);

        await _alertNotifier.Received(1).NotifyAsync(
            7,
            Arg.Is<AlertResponse>(a => a.Id == 55 && a.ContainerId == 1 && a.NivelRiesgo == "CRITICAL"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecalcularAsync_WhenAlertCreatedButContainerHasNoAssignedAnalyst_DoesNotNotify()
    {
        var container = new Container
        {
            Id = 1,
            NumeroContainer = "MSCU1234567",
            FechaArriboReal = DateTimeOffset.UtcNow.AddDays(-1),
            DeadlineDemurrage = DateTimeOffset.UtcNow.AddHours(24),
            AsignadoA = null,
        };

        await _sut.RecalcularAsync(container, CancellationToken.None);

        await _alertNotifier.DidNotReceive().NotifyAsync(Arg.Any<int>(), Arg.Any<AlertResponse>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecalcularAsync_ByContainerId_WhenContainerDoesNotExist_ReturnsNotFound()
    {
        _containerRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Container?)null);

        var result = await _sut.RecalcularAsync(99, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
