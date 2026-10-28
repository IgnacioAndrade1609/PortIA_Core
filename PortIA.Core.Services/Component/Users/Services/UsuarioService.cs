namespace PortIA.Core.Services.Component.Users.Services;

using FluentValidation;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Common.Security;
using PortIA.Core.Services.Component.Users.DTOs;
using PortIA.Core.Services.Component.Users.Interfaces;
using PortIA.Core.Services.Component.Users.Mappers;

public class UsuarioService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IValidator<CreateUsuarioRequest> validator,
    ILogger<UsuarioService> logger) : IUsuarioService
{
    public async Task<Result<UsuarioResponse>> CreateAsync(CreateUsuarioRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<UsuarioResponse>.ValidationError(validation.ToDictionary());

        var rol = await unitOfWork.Roles.GetByIdAsync(request.RolId, ct);
        if (rol is null)
        {
            logger.LogWarning("CreateUsuario rechazado: rolId={RolId} no existe", request.RolId);
            return Result<UsuarioResponse>.BusinessError("El rol especificado no existe.");
        }

        if (await unitOfWork.Usuarios.ExistsByEmailAsync(request.Email, ct))
        {
            logger.LogWarning("CreateUsuario rechazado: correo ya registrado (email={Email})", request.Email);
            return Result<UsuarioResponse>.BusinessError("Ya existe un usuario registrado con ese correo.");
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var usuario = request.ToEntity(passwordHash);
        usuario.Rol = rol;

        await unitOfWork.Usuarios.AddAsync(usuario, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Usuario creado (userId={UserId}, rolId={RolId})", usuario.Id, rol.Id);

        return Result<UsuarioResponse>.Success(usuario.ToResponse());
    }
}
