namespace PortIA.Core.Tests.Component.Terminales;

using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Terminales.DTOs;
using PortIA.Core.Services.Component.Terminales.Services;

public class TerminalServiceTests
{
    private readonly ITerminalRepository _terminalRepository;
    private readonly IPaisRepository _paisRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTerminalRequest> _createValidator;
    private readonly IValidator<UpdateTerminalRequest> _updateValidator;
    private readonly TerminalService _sut;

    public TerminalServiceTests()
    {
        _terminalRepository = Substitute.For<ITerminalRepository>();
        _paisRepository = Substitute.For<IPaisRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Terminales.Returns(_terminalRepository);
        _unitOfWork.Paises.Returns(_paisRepository);
        _createValidator = Substitute.For<IValidator<CreateTerminalRequest>>();
        _updateValidator = Substitute.For<IValidator<UpdateTerminalRequest>>();
        _createValidator.ValidateAsync(Arg.Any<CreateTerminalRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _updateValidator.ValidateAsync(Arg.Any<UpdateTerminalRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _paisRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Pais { Id = 1, NombrePais = "Chile" });
        _sut = new TerminalService(_unitOfWork, _createValidator, _updateValidator, Substitute.For<ILogger<TerminalService>>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ReturnsValidationError()
    {
        _createValidator.ValidateAsync(Arg.Any<CreateTerminalRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("CodigoTerminal", "El código del terminal es obligatorio.") }));

        var result = await _sut.CreateAsync(new CreateTerminalRequest("", "", null, null), CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithUnknownPais_ReturnsBusinessError()
    {
        var result = await _sut.CreateAsync(new CreateTerminalRequest("STI", "San Antonio", 99, null), CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithExistingCodigo_ReturnsConflict()
    {
        _terminalRepository.ExistsByCodigoAsync("STI", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new CreateTerminalRequest(" sti ", "San Antonio", 1, null), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsNormalizedTerminalWithPais()
    {
        var result = await _sut.CreateAsync(
            new CreateTerminalRequest(" sti ", " San Antonio ", 1, " https://sti.cl "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("STI", result.Value.CodigoTerminal);
        Assert.Equal("San Antonio", result.Value.NombreTerminal);
        Assert.Equal("Chile", result.Value.Pais);
        Assert.Equal("https://sti.cl", result.Value.ConfigScraper);
        Assert.True(result.Value.Vigente);
        await _terminalRepository.Received(1).AddAsync(Arg.Any<Terminal>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutPais_PersistsWithNullPais()
    {
        var result = await _sut.CreateAsync(new CreateTerminalRequest("XYZ", "Sin país", null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.IdPais);
        Assert.Null(result.Value.Pais);
    }

    [Fact]
    public async Task UpdateAsync_WhenTerminalDoesNotExist_ReturnsNotFound()
    {
        _terminalRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Terminal?)null);

        var result = await _sut.UpdateAsync(99, new UpdateTerminalRequest("STI", "San Antonio", 1, null), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_WithCodigoUsedByAnother_ReturnsConflict()
    {
        _terminalRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Terminal { Id = 1, CodigoTerminal = "DPW", NombreTerminal = "DP World" });
        _terminalRepository.ExistsByCodigoAsync("STI", 1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(1, new UpdateTerminalRequest("STI", "DP World", 1, null), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesAllFields()
    {
        var terminal = new Terminal { Id = 1, CodigoTerminal = "DPW", NombreTerminal = "DP World", Vigente = true };
        _terminalRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(terminal);

        var result = await _sut.UpdateAsync(
            1, new UpdateTerminalRequest("dpx", "DP World SAI", 1, "https://dp.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("DPX", terminal.CodigoTerminal);
        Assert.Equal("DP World SAI", terminal.NombreTerminal);
        Assert.Equal(1, terminal.IdPais);
        Assert.Equal("https://dp.com", terminal.ConfigScraper);
        Assert.Equal("Chile", result.Value.Pais);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateVigenciaAsync_WhenTerminalDoesNotExist_ReturnsNotFound()
    {
        _terminalRepository.GetByIdWithPaisAsync(99, Arg.Any<CancellationToken>()).Returns((Terminal?)null);

        var result = await _sut.UpdateVigenciaAsync(99, new UpdateTerminalVigenciaRequest { Vigente = false }, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateVigenciaAsync_WithExistingTerminal_TogglesVigencia()
    {
        var terminal = new Terminal { Id = 1, CodigoTerminal = "STI", NombreTerminal = "San Antonio", Vigente = true };
        _terminalRepository.GetByIdWithPaisAsync(1, Arg.Any<CancellationToken>()).Returns(terminal);

        var result = await _sut.UpdateVigenciaAsync(1, new UpdateTerminalVigenciaRequest { Vigente = false }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(terminal.Vigente);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedTerminalesWithPais()
    {
        _terminalRepository.GetAllWithPaisAsync(Arg.Any<CancellationToken>()).Returns(new List<Terminal>
        {
            new() { Id = 1, CodigoTerminal = "STI", NombreTerminal = "San Antonio", IdPais = 1, Pais = new Pais { Id = 1, NombrePais = "Chile" } },
            new() { Id = 2, CodigoTerminal = "XYZ", NombreTerminal = "Sin país" }
        });

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        var lista = result.Value.ToList();
        Assert.Equal("Chile", lista[0].Pais);
        Assert.Null(lista[1].Pais);
    }

    [Fact]
    public async Task GetPaisesAsync_ReturnsMappedPaises()
    {
        _paisRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Pais> { new() { Id = 1, NombrePais = "Chile" } });

        var result = await _sut.GetPaisesAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Chile", result.Value.Single().NombrePais);
    }
}
