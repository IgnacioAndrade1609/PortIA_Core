namespace PortIA.Core.Tests.Component.Mensajeria;

using MassTransit;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Mensajeria.Consumers;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;

public class DataExtractedConsumerTests
{
    private readonly IContainerRepository _containerRepository;
    private readonly IHitoRepository _hitoRepository;
    private readonly ITerminalRepository _terminalRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInspectContainerPublisher _inspectContainerPublisher;
    private readonly DataExtractedConsumer _sut;

    private static readonly List<Terminal> TerminalesSeed =
    [
        new() { Id = 1, CodigoTerminal = "STI", NombreTerminal = "San Antonio Terminal Internacional" },
        new() { Id = 2, CodigoTerminal = "TPS", NombreTerminal = "Terminal Pacífico Sur Valparaíso" },
        new() { Id = 3, CodigoTerminal = "DPW", NombreTerminal = "DP World San Antonio" },
    ];

    public DataExtractedConsumerTests()
    {
        _containerRepository = Substitute.For<IContainerRepository>();
        _hitoRepository = Substitute.For<IHitoRepository>();
        _terminalRepository = Substitute.For<ITerminalRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Containers.Returns(_containerRepository);
        _unitOfWork.Hitos.Returns(_hitoRepository);
        _unitOfWork.Terminales.Returns(_terminalRepository);

        SetTerminalesQueryResult(TerminalesSeed);

        _containerRepository
            .AddAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => callInfo.Arg<Container>().Id = 10);

        _inspectContainerPublisher = Substitute.For<IInspectContainerPublisher>();
        _inspectContainerPublisher.PublishAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        _sut = new DataExtractedConsumer(_unitOfWork, _inspectContainerPublisher, Substitute.For<ILogger<DataExtractedConsumer>>());
    }

    private void SetTerminalesQueryResult(List<Terminal> terminales)
    {
        var mockQueryable = terminales.BuildMockDbSet();
        _terminalRepository.Query().Returns(mockQueryable);
    }

    private static ConsumeContext<DataExtracted> BuildContext(DataExtracted evento)
    {
        var context = Substitute.For<ConsumeContext<DataExtracted>>();
        context.Message.Returns(evento);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private static DataExtracted BuildEvento() => new(
        ShipmentId: 1,
        DocumentId: 1,
        DocumentType: "BL",
        BlNumber: "BL-1",
        ContainerNumber: "MSCU1234567",
        ShippingLine: "MSC",
        PortOfDestination: "Valparaíso",
        Eta: DateTimeOffset.UtcNow,
        DemurrageFreeDays: 7,
        DetentionFreeDays: 5,
        ForwarderName: "Agencia",
        ExtractedDataRaw: new Dictionary<string, object>(),
        Confidence: 0.95,
        ExtractionStatus: "OK");

    [Fact]
    public async Task Consume_CreatesContainerAndHito()
    {
        await _sut.Consume(BuildContext(BuildEvento()));

        await _containerRepository.Received(1).AddAsync(
            Arg.Is<Container>(c => c.NumeroContainer == "MSCU1234567"), Arg.Any<CancellationToken>());
        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 10 && h.Origen == "IA"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_AfterCreatingContainer_TriggersInspection()
    {
        await _sut.Consume(BuildContext(BuildEvento()));

        await _inspectContainerPublisher.Received(1).PublishAsync(10, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Puerto de Valparaíso", 2)]
    [InlineData("Valparaíso", 2)]
    public async Task Consume_WithUnambiguousPort_AssignsMatchingTerminal(string puertoDestino, int idTerminalEsperado)
    {
        var evento = BuildEvento() with { PortOfDestination = puertoDestino };

        await _sut.Consume(BuildContext(evento));

        await _containerRepository.Received(1).AddAsync(
            Arg.Is<Container>(c => c.IdTerminalLlegada == idTerminalEsperado), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithAmbiguousPort_AssignsFirstMatchingTerminal()
    {
        var evento = BuildEvento() with { PortOfDestination = "Puerto de San Antonio" };

        await _sut.Consume(BuildContext(evento));

        await _containerRepository.Received(1).AddAsync(
            Arg.Is<Container>(c => c.IdTerminalLlegada == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithUnknownPort_LeavesTerminalLlegadaNull()
    {
        var evento = BuildEvento() with { PortOfDestination = "Puerto del Callao" };

        await _sut.Consume(BuildContext(evento));

        await _containerRepository.Received(1).AddAsync(
            Arg.Is<Container>(c => c.IdTerminalLlegada == null), Arg.Any<CancellationToken>());
    }
}
