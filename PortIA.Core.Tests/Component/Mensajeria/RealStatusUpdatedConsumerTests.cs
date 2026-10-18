namespace PortIA.Core.Tests.Component.Mensajeria;

using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Mensajeria.Consumers;
using PortIA.Core.Services.Component.Mensajeria.Events;

public class RealStatusUpdatedConsumerTests
{
    private readonly IContainerRepository _containerRepository;
    private readonly IClimaRepository _climaRepository;
    private readonly IAccionRepository _accionRepository;
    private readonly ILogAuditoriaRepository _logAuditoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RealStatusUpdatedConsumer _sut;

    public RealStatusUpdatedConsumerTests()
    {
        _containerRepository = Substitute.For<IContainerRepository>();
        _climaRepository = Substitute.For<IClimaRepository>();
        _accionRepository = Substitute.For<IAccionRepository>();
        _logAuditoriaRepository = Substitute.For<ILogAuditoriaRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Containers.Returns(_containerRepository);
        _unitOfWork.Climas.Returns(_climaRepository);
        _unitOfWork.Acciones.Returns(_accionRepository);
        _unitOfWork.LogsAuditoria.Returns(_logAuditoriaRepository);

        _climaRepository.GetByDescripcionAsync("Sin alerta", Arg.Any<CancellationToken>())
            .Returns(new Clima { Id = 1, Descripcion = "Sin alerta" });
        _climaRepository.GetByDescripcionAsync("Marejada", Arg.Any<CancellationToken>())
            .Returns(new Clima { Id = 2, Descripcion = "Marejada" });
        _accionRepository.GetByDescripcionAsync("ACTUALIZAR", Arg.Any<CancellationToken>())
            .Returns(new Accion { Id = 2, Descripcion = "ACTUALIZAR" });

        _sut = new RealStatusUpdatedConsumer(_unitOfWork, Substitute.For<ILogger<RealStatusUpdatedConsumer>>());
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
}
