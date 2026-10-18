namespace PortIA.Core.Tests.Component.Mensajeria;

using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Mensajeria.Publishers;

public class InspectContainerPublisherTests
{
    private readonly IContainerRepository _containerRepository;
    private readonly ITerminalRepository _terminalRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly InspectContainerPublisher _sut;

    public InspectContainerPublisherTests()
    {
        _containerRepository = Substitute.For<IContainerRepository>();
        _terminalRepository = Substitute.For<ITerminalRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Containers.Returns(_containerRepository);
        _unitOfWork.Terminales.Returns(_terminalRepository);

        _publishEndpoint = Substitute.For<IPublishEndpoint>();

        _sut = new InspectContainerPublisher(_unitOfWork, _publishEndpoint, Substitute.For<ILogger<InspectContainerPublisher>>());
    }

    [Fact]
    public async Task PublishAsync_WhenContainerDoesNotExist_ReturnsNotFound()
    {
        _containerRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Container?)null);

        var result = await _sut.PublishAsync(99, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<InspectContainer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WhenContainerHasNoTerminalAssigned_ReturnsBusinessError()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", IdTerminalLlegada = null };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.PublishAsync(1, CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<InspectContainer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WhenTerminalDoesNotExist_ReturnsBusinessError()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", IdTerminalLlegada = 2 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);
        _terminalRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns((Terminal?)null);

        var result = await _sut.PublishAsync(1, CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
    }

    [Fact]
    public async Task PublishAsync_WhenTerminalExists_PublishesInspectContainerEvent()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", IdTerminalLlegada = 2 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var terminal = new Terminal { Id = 2, CodigoTerminal = "STI", NombreTerminal = "San Antonio", ConfigScraper = "https://sti.cl" };
        _terminalRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(terminal);

        var result = await _sut.PublishAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<InspectContainer>(e => e.ContainerId == 1 && e.ContainerNumber == "MSCU1234567" && e.TerminalCode == "STI"),
            Arg.Any<CancellationToken>());
    }
}
