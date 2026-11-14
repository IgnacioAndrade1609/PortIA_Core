namespace PortIA.Core.Tests.Component.Navieras;

using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Navieras.DTOs;
using PortIA.Core.Services.Component.Navieras.Services;

public class NavieraServiceTests
{
    private readonly INavieraRepository _navieraRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateNavieraRequest> _createValidator;
    private readonly IValidator<UpdateNavieraRequest> _updateValidator;
    private readonly NavieraService _sut;

    public NavieraServiceTests()
    {
        _navieraRepository = Substitute.For<INavieraRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Navieras.Returns(_navieraRepository);
        _createValidator = Substitute.For<IValidator<CreateNavieraRequest>>();
        _updateValidator = Substitute.For<IValidator<UpdateNavieraRequest>>();
        _createValidator.ValidateAsync(Arg.Any<CreateNavieraRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _updateValidator.ValidateAsync(Arg.Any<UpdateNavieraRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _sut = new NavieraService(_unitOfWork, _createValidator, _updateValidator, Substitute.For<ILogger<NavieraService>>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ReturnsValidationError()
    {
        _createValidator.ValidateAsync(Arg.Any<CreateNavieraRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("NombreNaviera", "El nombre de la naviera es obligatorio.") }));

        var result = await _sut.CreateAsync(new CreateNavieraRequest("", null, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithExistingNombre_ReturnsConflict()
    {
        _navieraRepository.ExistsByNombreAsync("Maersk", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new CreateNavieraRequest(" Maersk ", "MAEU", null, null), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithExistingCodigo_ReturnsConflict()
    {
        _navieraRepository.ExistsByCodigoAsync("MAEU", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new CreateNavieraRequest("Maersk", "MAEU", null, null), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsAndReturnsVigenteNaviera()
    {
        var result = await _sut.CreateAsync(
            new CreateNavieraRequest(" Maersk ", " MAEU ", "", " 123 "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Maersk", result.Value.NombreNaviera);
        Assert.Equal("MAEU", result.Value.CodigoNaviera);
        Assert.Null(result.Value.Email);
        Assert.Equal("123", result.Value.Telefono);
        Assert.True(result.Value.Vigente);
        await _navieraRepository.Received(1).AddAsync(Arg.Any<Naviera>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNavieraDoesNotExist_ReturnsNotFound()
    {
        _navieraRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Naviera?)null);

        var result = await _sut.UpdateAsync(99, new UpdateNavieraRequest("Maersk", null, null, null), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_WithNombreUsedByAnother_ReturnsConflict()
    {
        _navieraRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Naviera { Id = 1, NombreNaviera = "MSC" });
        _navieraRepository.ExistsByNombreAsync("Maersk", 1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(1, new UpdateNavieraRequest("Maersk", null, null, null), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesAllFields()
    {
        var naviera = new Naviera { Id = 1, NombreNaviera = "MSC", CodigoNaviera = "MSCU", Vigente = true };
        _navieraRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(naviera);

        var result = await _sut.UpdateAsync(
            1, new UpdateNavieraRequest("MSC Cruises", "MSCC", "ops@msc.com", "555"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("MSC Cruises", naviera.NombreNaviera);
        Assert.Equal("MSCC", naviera.CodigoNaviera);
        Assert.Equal("ops@msc.com", naviera.Email);
        Assert.Equal("555", naviera.Telefono);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateVigenciaAsync_WhenNavieraDoesNotExist_ReturnsNotFound()
    {
        _navieraRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Naviera?)null);

        var result = await _sut.UpdateVigenciaAsync(99, new UpdateNavieraVigenciaRequest { Vigente = false }, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateVigenciaAsync_WithExistingNaviera_TogglesVigencia()
    {
        var naviera = new Naviera { Id = 1, NombreNaviera = "MSC", Vigente = true };
        _navieraRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(naviera);

        var result = await _sut.UpdateVigenciaAsync(1, new UpdateNavieraVigenciaRequest { Vigente = false }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(naviera.Vigente);
        Assert.False(result.Value.Vigente);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenNavieraDoesNotExist_ReturnsNotFound()
    {
        _navieraRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Naviera?)null);

        var result = await _sut.GetByIdAsync(99, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedNavieras()
    {
        _navieraRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Naviera>
        {
            new() { Id = 1, NombreNaviera = "MSC", CodigoNaviera = "MSCU", Vigente = true },
            new() { Id = 2, NombreNaviera = "Maersk", Vigente = false }
        });

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }
}
