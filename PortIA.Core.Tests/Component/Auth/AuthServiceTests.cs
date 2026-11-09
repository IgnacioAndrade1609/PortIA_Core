namespace PortIA.Core.Tests.Component.Auth;

using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Common.Security;
using PortIA.Core.Services.Component.Auth.DTOs;
using PortIA.Core.Services.Component.Auth.Services;

public class AuthServiceTests
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<LoginRequest> _validator;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _usuarioRepository = Substitute.For<IUsuarioRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Usuarios.Returns(_usuarioRepository);

        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _validator = Substitute.For<IValidator<LoginRequest>>();

        _sut = new AuthService(_unitOfWork, _passwordHasher, _jwtTokenGenerator, _validator, Substitute.For<ILogger<AuthService>>());

        SetValidationResult(isValid: true);
    }

    private void SetValidationResult(bool isValid, string? propertyName = null, string? errorMessage = null)
    {
        var failures = isValid
            ? []
            : new List<ValidationFailure> { new(propertyName ?? "Email", errorMessage ?? "Inválido") };

        _validator
            .ValidateAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));
    }

    private static LoginRequest BuildRequest() => new() { Email = "juan.perez@portia.com", Password = "Password123!" };

    private static Usuario BuildUsuario(string passwordHash) => new()
    {
        Id = 1,
        NombreUsuario = "Juan Pérez",
        Email = "juan.perez@portia.com",
        PasswordHash = passwordHash,
        Vigente = true,
        Rol = new Rol { Id = 2, NombreRol = "Analista" },
    };

    [Fact]
    public async Task LoginAsync_WithInvalidRequest_ReturnsValidationError()
    {
        SetValidationResult(isValid: false, propertyName: "Password", errorMessage: "La contraseña es obligatoria.");

        var result = await _sut.LoginAsync(BuildRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
    }

    [Fact]
    public async Task LoginAsync_WhenUsuarioDoesNotExist_ReturnsUnauthorized()
    {
        _usuarioRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Usuario?)null);

        var result = await _sut.LoginAsync(BuildRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task LoginAsync_WhenUsuarioIsNotVigente_ReturnsUnauthorized()
    {
        var usuario = BuildUsuario("hash");
        usuario.Vigente = false;
        _usuarioRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(usuario);

        var result = await _sut.LoginAsync(BuildRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsUnauthorized()
    {
        var usuario = BuildUsuario("hash");
        _usuarioRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(usuario);
        _passwordHasher.Verify(Arg.Any<string>(), usuario.PasswordHash).Returns(false);

        var result = await _sut.LoginAsync(BuildRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        var usuario = BuildUsuario("hash");
        var tokenResult = new JwtTokenResult("token-jwt", DateTimeOffset.UtcNow.AddHours(1));

        _usuarioRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(usuario);
        _passwordHasher.Verify(Arg.Any<string>(), usuario.PasswordHash).Returns(true);
        _jwtTokenGenerator.GenerateToken(usuario.Id, usuario.Email, usuario.Rol.NombreRol).Returns(tokenResult);

        var result = await _sut.LoginAsync(BuildRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tokenResult.Token, result.Value.Token);
        Assert.Equal(usuario.Id, result.Value.UsuarioId);
        Assert.Equal(usuario.Rol.NombreRol, result.Value.Rol);
    }
}
