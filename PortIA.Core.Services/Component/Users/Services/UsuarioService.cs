namespace PortIA.Core.Services.Component.Users.Services;

using FluentValidation;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Common.Security;
using PortIA.Core.Services.Component.Users.DTOs;
using PortIA.Core.Services.Component.Users.Interfaces;
using PortIA.Core.Services.Component.Users.Mappers;

public class UsuarioService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IValidator<CreateUsuarioRequest> createValidator,
    IValidator<UpdateUsuarioRequest> updateValidator,
    IValidator<UpdateUsuarioRoleRequest> roleValidator,
    ILogger<UsuarioService> logger) : IUsuarioService
{
    public async Task<Result<PagedResult<UserDto>>> GetPagedAsync(UserQueryFilter filter, CancellationToken ct)
    {
        // 1. Recibimos la tupla de datos crudos y el conteo total desde la infraestructura
        var (items, totalItems) = await unitOfWork.Usuarios.GetPagedAsync(
            filter.Search, 
            filter.RoleId, 
            filter.Active, 
            filter.Page, 
            filter.PageSize, 
            ct);

        // 2. Mapeamos las entidades a DTOs
        var dtos = items.Select(u => u.ToDto()).ToList();

        // 3. Calculamos el total de páginas de forma segura
        int totalPages = filter.PageSize > 0 
            ? (int)Math.Ceiling(totalItems / (double)filter.PageSize) 
            : 1;

        // 4. Construimos el PagedResult
        var pagedResult = new PagedResult<UserDto>
        {
            Items = dtos,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages == 0 ? 1 : totalPages
        };

        return Result<PagedResult<UserDto>>.Success(pagedResult);
    }

    public async Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var usuario = await unitOfWork.Usuarios.GetByIdWithRoleAsync(id, ct);
        if (usuario is null)
        {
            return Result<UserDto>.NotFound($"No se encontró el usuario con ID {id}.");
        }

        return Result<UserDto>.Success(usuario.ToDto());
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUsuarioRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<UserDto>.ValidationError(validation.ToDictionary());

        var rol = await unitOfWork.Roles.GetByIdAsync(request.RolId, ct);
        if (rol is null)
        {
            logger.LogWarning("CreateUsuario rechazado: rolId={RolId} no existe", request.RolId);
            return Result<UserDto>.BusinessError("El rol especificado no existe.");
        }

        if (await unitOfWork.Usuarios.ExistsByEmailAsync(request.Email, ct))
        {
            logger.LogWarning("CreateUsuario rechazado: correo ya registrado (email={Email})", request.Email);
            return Result<UserDto>.Conflict("Ya existe un usuario registrado con ese correo.");
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var usuario = request.ToEntity(passwordHash);
        usuario.Rol = rol;

        await unitOfWork.Usuarios.AddAsync(usuario, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Usuario creado (userId={UserId}, rolId={RolId})", usuario.Id, rol.Id);

        return Result<UserDto>.Success(usuario.ToDto());
    }

    public async Task<Result<UserDto>> UpdateAsync(int id, UpdateUsuarioRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<UserDto>.ValidationError(validation.ToDictionary());

        var usuario = await unitOfWork.Usuarios.GetByIdWithRoleAsync(id, ct);
        if (usuario is null)
            return Result<UserDto>.NotFound($"No se encontró el usuario con ID {id}.");

        if (!usuario.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase) &&
            await unitOfWork.Usuarios.ExistsByEmailAsync(request.Email, ct))
        {
            logger.LogWarning("UpdateUsuario rechazado: correo ya registrado (email={Email})", request.Email);
            return Result<UserDto>.Conflict("Ya existe un usuario registrado con ese correo.");
        }

        usuario.NombreUsuario = request.Name;
        usuario.Email = request.Email;

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Usuario actualizado (userId={UserId})", usuario.Id);

        return Result<UserDto>.Success(usuario.ToDto());
    }

    public async Task<Result<UserDto>> UpdateStatusAsync(int id, UpdateUsuarioStatusRequest request, int currentUserId, CancellationToken ct)
    {
        if (id == currentUserId && !request.Active)
        {
            logger.LogWarning("UpdateUsuarioStatus rechazado: Admin intentó desactivarse a sí mismo (userId={UserId})", id);
            return Result<UserDto>.Conflict("Un administrador no puede desactivar su propio usuario.");
        }

        var usuario = await unitOfWork.Usuarios.GetByIdWithRoleAsync(id, ct);
        if (usuario is null)
            return Result<UserDto>.NotFound($"No se encontró el usuario con ID {id}.");

        usuario.Vigente = request.Active;

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Estado de usuario actualizado (userId={UserId}, active={Active})", usuario.Id, usuario.Vigente);

        return Result<UserDto>.Success(usuario.ToDto());
    }

    public async Task<Result<UserDto>> UpdateRoleAsync(int id, UpdateUsuarioRoleRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await roleValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<UserDto>.ValidationError(validation.ToDictionary());

        if (id == currentUserId)
        {
            logger.LogWarning("UpdateUsuarioRole rechazado: Admin intentó cambiar su propio rol (userId={UserId})", id);
            return Result<UserDto>.Conflict("Un administrador no puede modificar su propio rol.");
        }

        var usuario = await unitOfWork.Usuarios.GetByIdWithRoleAsync(id, ct);
        if (usuario is null)
            return Result<UserDto>.NotFound($"No se encontró el usuario con ID {id}.");

        var rol = await unitOfWork.Roles.GetByIdAsync(request.RoleId, ct);
        if (rol is null)
        {
            logger.LogWarning("UpdateUsuarioRole rechazado: rolId={RolId} no existe", request.RoleId);
            return Result<UserDto>.BusinessError("El rol especificado no existe.");
        }

        usuario.RolId = rol.Id;
        usuario.Rol = rol;

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Rol de usuario actualizado (userId={UserId}, newRoleId={RolId})", usuario.Id, rol.Id);

        return Result<UserDto>.Success(usuario.ToDto());
    }

    public async Task<Result<IEnumerable<RoleDto>>> GetRolesAsync(CancellationToken ct)
    {
        var roles = await unitOfWork.Roles.GetAllAsync(ct);
        var dtos = roles.Select(r => r.ToDto());
        return Result<IEnumerable<RoleDto>>.Success(dtos);
    }
}