namespace PortIA.Core.Tests.Component.Containers;

using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Containers.Services;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;

public class ContainerServiceTests
{
    private readonly IContainerRepository _containerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInspectContainerPublisher _inspectContainerPublisher;
    private readonly ContainerService _sut;

    public ContainerServiceTests()
    {
        _containerRepository = Substitute.For<IContainerRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Containers.Returns(_containerRepository);

        _inspectContainerPublisher = Substitute.For<IInspectContainerPublisher>();

        _sut = new ContainerService(_unitOfWork, _inspectContainerPublisher, Substitute.For<ILogger<ContainerService>>());
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

        var result = await _sut.GetHitosAsync(1, userId: 7, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetHitosAsync_WhenContainerBelongsToAnotherUser_ReturnsNotFound()
    {
        var container = new Container { Id = 1, NumeroContainer = "MSCU1234567", AsignadoA = 7 };
        _containerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(container);

        var result = await _sut.GetHitosAsync(1, userId: 99, CancellationToken.None);

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

        var result = await _sut.GetHitosAsync(1, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("Primero", result.Value[0].Descripcion);
        Assert.Equal("Segundo", result.Value[1].Descripcion);
    }
}
