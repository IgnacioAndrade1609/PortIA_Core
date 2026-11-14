namespace PortIA.Core.Services.Component.Navieras.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Navieras.DTOs;

public interface INavieraService
{
    Task<Result<IEnumerable<NavieraDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<NavieraDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<NavieraDto>> CreateAsync(CreateNavieraRequest request, CancellationToken ct);
    Task<Result<NavieraDto>> UpdateAsync(int id, UpdateNavieraRequest request, CancellationToken ct);
    Task<Result<NavieraDto>> UpdateVigenciaAsync(int id, UpdateNavieraVigenciaRequest request, CancellationToken ct);
}
