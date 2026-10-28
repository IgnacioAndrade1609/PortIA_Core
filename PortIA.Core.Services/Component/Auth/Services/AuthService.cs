namespace PortIA.Core.Services.Component.Auth.Services;

using FluentValidation;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Common.Security;
using PortIA.Core.Services.Component.Auth.DTOs;
using PortIA.Core.Services.Component.Auth.Interfaces;
using PortIA.Core.Services.Component.Auth.Mappers;

public class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IValidator<LoginRequest> validator,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentialsMessage = "Credenciales inválidas.";

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<LoginResponse>.ValidationError(validation.ToDictionary());

        var usuario = await unitOfWork.Usuarios.GetByEmailAsync(request.Email, ct);

        if (usuario is null || !usuario.Vigente)
        {
            logger.LogWarning("Login fallido: usuario no existe o está inactivo (email={Email})", request.Email);
            return Result<LoginResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        if (!passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            logger.LogWarning("Login fallido: contraseña incorrecta (userId={UserId})", usuario.Id);
            return Result<LoginResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        var token = jwtTokenGenerator.GenerateToken(usuario.Id, usuario.Email, usuario.Rol.NombreRol);

        logger.LogInformation("Login exitoso (userId={UserId}, rol={Rol})", usuario.Id, usuario.Rol.NombreRol);

        return Result<LoginResponse>.Success(usuario.ToLoginResponse(token));
    }
}
