namespace PortIA.Core.Services.Component.Navieras.Services;

using FluentValidation;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Navieras.DTOs;
using PortIA.Core.Services.Component.Navieras.Interfaces;
using PortIA.Core.Services.Component.Navieras.Mappers;

public class NavieraService(
    IUnitOfWork unitOfWork,
    IValidator<CreateNavieraRequest> createValidator,
    IValidator<UpdateNavieraRequest> updateValidator,
    ILogger<NavieraService> logger) : INavieraService
{
    public async Task<Result<IEnumerable<NavieraDto>>> GetAllAsync(CancellationToken ct)
    {
        var navieras = await unitOfWork.Navieras.GetAllAsync(ct);
        return Result<IEnumerable<NavieraDto>>.Success(navieras.Select(n => n.ToDto()).ToList());
    }

    public async Task<Result<NavieraDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var naviera = await unitOfWork.Navieras.GetByIdAsync(id, ct);
        if (naviera is null)
            return Result<NavieraDto>.NotFound($"No se encontró la naviera con ID {id}.");

        return Result<NavieraDto>.Success(naviera.ToDto());
    }

    public async Task<Result<NavieraDto>> CreateAsync(CreateNavieraRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<NavieraDto>.ValidationError(validation.ToDictionary());

        var nombre = request.NombreNaviera.Trim();
        var codigo = NavieraMapper.Normalizar(request.CodigoNaviera);

        if (await unitOfWork.Navieras.ExistsByNombreAsync(nombre, null, ct))
        {
            logger.LogWarning("CreateNaviera rechazado: nombre ya registrado (nombre={Nombre})", nombre);
            return Result<NavieraDto>.Conflict("Ya existe una naviera con ese nombre.");
        }

        if (codigo is not null && await unitOfWork.Navieras.ExistsByCodigoAsync(codigo, null, ct))
        {
            logger.LogWarning("CreateNaviera rechazado: código ya registrado (codigo={Codigo})", codigo);
            return Result<NavieraDto>.Conflict("Ya existe una naviera con ese código.");
        }

        var naviera = request.ToEntity();
        await unitOfWork.Navieras.AddAsync(naviera, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Naviera creada (navieraId={NavieraId})", naviera.Id);

        return Result<NavieraDto>.Success(naviera.ToDto());
    }

    public async Task<Result<NavieraDto>> UpdateAsync(int id, UpdateNavieraRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<NavieraDto>.ValidationError(validation.ToDictionary());

        var naviera = await unitOfWork.Navieras.GetByIdAsync(id, ct);
        if (naviera is null)
            return Result<NavieraDto>.NotFound($"No se encontró la naviera con ID {id}.");

        var nombre = request.NombreNaviera.Trim();
        var codigo = NavieraMapper.Normalizar(request.CodigoNaviera);

        if (await unitOfWork.Navieras.ExistsByNombreAsync(nombre, id, ct))
        {
            logger.LogWarning("UpdateNaviera rechazado: nombre ya registrado (navieraId={NavieraId}, nombre={Nombre})", id, nombre);
            return Result<NavieraDto>.Conflict("Ya existe una naviera con ese nombre.");
        }

        if (codigo is not null && await unitOfWork.Navieras.ExistsByCodigoAsync(codigo, id, ct))
        {
            logger.LogWarning("UpdateNaviera rechazado: código ya registrado (navieraId={NavieraId}, codigo={Codigo})", id, codigo);
            return Result<NavieraDto>.Conflict("Ya existe una naviera con ese código.");
        }

        naviera.Apply(request);

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Naviera actualizada (navieraId={NavieraId})", naviera.Id);

        return Result<NavieraDto>.Success(naviera.ToDto());
    }

    public async Task<Result<NavieraDto>> UpdateVigenciaAsync(int id, UpdateNavieraVigenciaRequest request, CancellationToken ct)
    {
        var naviera = await unitOfWork.Navieras.GetByIdAsync(id, ct);
        if (naviera is null)
            return Result<NavieraDto>.NotFound($"No se encontró la naviera con ID {id}.");

        naviera.Vigente = request.Vigente;

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Vigencia de naviera actualizada (navieraId={NavieraId}, vigente={Vigente})", naviera.Id, naviera.Vigente);

        return Result<NavieraDto>.Success(naviera.ToDto());
    }
}
