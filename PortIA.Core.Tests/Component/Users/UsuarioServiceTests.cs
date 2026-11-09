namespace PortIA.Core.Tests.Component.Users;

using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Common.Security;
using PortIA.Core.Services.Component.Users.DTOs;
using PortIA.Core.Services.Component.Users.Services;

public class UsuarioServiceTests
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateUsuarioRequest> _validator;
    private readonly UsuarioService _sut;

    public UsuarioServiceTests()
    {
        _usuarioRepository = Substitute.For<IUsuarioRepository>();
        _rolRepository = Substitute.For<IRolRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Usuarios.Returns(_usuarioRepository);
        _unitOfWork.Roles.Returns(_rolRepository);

        _passwordHasher = Substitute.For<IPasswordHasher>();
        _validator = Substitute.For<IValidator<CreateUsuarioRequest>>();

        _sut = new UsuarioService(_unitOfWork, _passwordHasher, _validator, Substitute.For<ILogger<UsuarioService>>());

        SetValidationResult(isValid: true);
    }

    private void SetValidationResult(bool isValid, string? propertyName = null, string? errorMessage = null)
    {
        var failures = isValid
            ? []
            : new List<ValidationFailure> { new(propertyName ?? "Email", errorMessage ?? "Inválido") };

        _validator
            .ValidateAsync(Arg.Any<CreateUsuarioRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));
    }

    private static CreateUsuarioRequest BuildRequest() =>
        new("Juan Pérez", "juan.perez@portia.com", "Password123!", RolId: 2);

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ReturnsValidationError()
    {
        SetValidationResult(isValid: false, propertyName: "Email", errorMessage: "El correo es obligatorio.");

        var result = await _sut.CreateAsync(BuildRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        await _usuarioRepository.DidNotReceive().AddAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentRol_ReturnsBusinessError()
    {
        _rolRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns((Rol?)null);

        var result = await _sut.CreateAsync(BuildRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _usuarioRepository.DidNotReceive().AddAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsBusinessError()
    {
        _rolRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new Rol { Id = 2, NombreRol = "Analista" });
        _usuarioRepository.ExistsByEmailAsync(BuildRequest().Email, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(BuildRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.BusinessError, result.Status);
        await _usuarioRepository.DidNotReceive().AddAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccess()
    {
        var request = BuildRequest();
        var rol = new Rol { Id = 2, NombreRol = "Analista" };

        _rolRepository.GetByIdAsync(request.RolId, Arg.Any<CancellationToken>()).Returns(rol);
        _usuarioRepository.ExistsByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(request.Password).Returns("hashed-password");

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(request.Email, result.Value.Email);
        Assert.Equal(rol.NombreRol, result.Value.Rol);
        await _usuarioRepository.Received(1).AddAsync(
            Arg.Is<Usuario>(u => u.Email == request.Email && u.PasswordHash == "hashed-password"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
