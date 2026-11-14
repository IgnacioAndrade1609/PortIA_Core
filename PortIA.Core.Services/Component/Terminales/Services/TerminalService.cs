namespace PortIA.Core.Services.Component.Terminales.Services;

using FluentValidation;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Terminales.DTOs;
using PortIA.Core.Services.Component.Terminales.Interfaces;
using PortIA.Core.Services.Component.Terminales.Mappers;

public class TerminalService(
    IUnitOfWork unitOfWork,
    IValidator<CreateTerminalRequest> createValidator,
    IValidator<UpdateTerminalRequest> updateValidator,
    ILogger<TerminalService> logger) : ITerminalService
{
    public async Task<Result<IEnumerable<TerminalDto>>> GetAllAsync(CancellationToken ct)
    {
        var terminales = await unitOfWork.Terminales.GetAllWithPaisAsync(ct);
        return Result<IEnumerable<TerminalDto>>.Success(terminales.Select(t => t.ToDto()).ToList());
    }

    public async Task<Result<TerminalDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var terminal = await unitOfWork.Terminales.GetByIdWithPaisAsync(id, ct);
        if (terminal is null)
            return Result<TerminalDto>.NotFound($"No se encontró el terminal con ID {id}.");

        return Result<TerminalDto>.Success(terminal.ToDto());
    }

    public async Task<Result<TerminalDto>> CreateAsync(CreateTerminalRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<TerminalDto>.ValidationError(validation.ToDictionary());

        var codigo = TerminalMapper.NormalizarCodigo(request.CodigoTerminal);

        string? nombrePais = null;
        if (request.IdPais.HasValue)
        {
            var pais = await unitOfWork.Paises.GetByIdAsync(request.IdPais.Value, ct);
            if (pais is null)
            {
                logger.LogWarning("CreateTerminal rechazado: paisId={PaisId} no existe", request.IdPais);
                return Result<TerminalDto>.BusinessError("El país especificado no existe.");
            }
            nombrePais = pais.NombrePais;
        }

        if (await unitOfWork.Terminales.ExistsByCodigoAsync(codigo, null, ct))
        {
            logger.LogWarning("CreateTerminal rechazado: código ya registrado (codigo={Codigo})", codigo);
            return Result<TerminalDto>.Conflict("Ya existe un terminal con ese código.");
        }

        var terminal = request.ToEntity();
        await unitOfWork.Terminales.AddAsync(terminal, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Terminal creado (terminalId={TerminalId})", terminal.Id);

        return Result<TerminalDto>.Success(terminal.ToDto(nombrePais));
    }

    public async Task<Result<TerminalDto>> UpdateAsync(int id, UpdateTerminalRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<TerminalDto>.ValidationError(validation.ToDictionary());

        var terminal = await unitOfWork.Terminales.GetByIdAsync(id, ct);
        if (terminal is null)
            return Result<TerminalDto>.NotFound($"No se encontró el terminal con ID {id}.");

        var codigo = TerminalMapper.NormalizarCodigo(request.CodigoTerminal);

        string? nombrePais = null;
        if (request.IdPais.HasValue)
        {
            var pais = await unitOfWork.Paises.GetByIdAsync(request.IdPais.Value, ct);
            if (pais is null)
            {
                logger.LogWarning("UpdateTerminal rechazado: paisId={PaisId} no existe", request.IdPais);
                return Result<TerminalDto>.BusinessError("El país especificado no existe.");
            }
            nombrePais = pais.NombrePais;
        }

        if (await unitOfWork.Terminales.ExistsByCodigoAsync(codigo, id, ct))
        {
            logger.LogWarning("UpdateTerminal rechazado: código ya registrado (terminalId={TerminalId}, codigo={Codigo})", id, codigo);
            return Result<TerminalDto>.Conflict("Ya existe un terminal con ese código.");
        }

        terminal.Apply(request);

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Terminal actualizado (terminalId={TerminalId})", terminal.Id);

        return Result<TerminalDto>.Success(terminal.ToDto(nombrePais));
    }

    public async Task<Result<TerminalDto>> UpdateVigenciaAsync(int id, UpdateTerminalVigenciaRequest request, CancellationToken ct)
    {
        var terminal = await unitOfWork.Terminales.GetByIdWithPaisAsync(id, ct);
        if (terminal is null)
            return Result<TerminalDto>.NotFound($"No se encontró el terminal con ID {id}.");

        terminal.Vigente = request.Vigente;

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Vigencia de terminal actualizada (terminalId={TerminalId}, vigente={Vigente})", terminal.Id, terminal.Vigente);

        return Result<TerminalDto>.Success(terminal.ToDto());
    }

    public async Task<Result<IEnumerable<PaisDto>>> GetPaisesAsync(CancellationToken ct)
    {
        var paises = await unitOfWork.Paises.GetAllAsync(ct);
        return Result<IEnumerable<PaisDto>>.Success(paises.Select(p => p.ToDto()).ToList());
    }
}
