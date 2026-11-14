namespace PortIA.Core.Services.Component.Terminales.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Terminales.DTOs;

public interface ITerminalService
{
    Task<Result<IEnumerable<TerminalDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<TerminalDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<TerminalDto>> CreateAsync(CreateTerminalRequest request, CancellationToken ct);
    Task<Result<TerminalDto>> UpdateAsync(int id, UpdateTerminalRequest request, CancellationToken ct);
    Task<Result<TerminalDto>> UpdateVigenciaAsync(int id, UpdateTerminalVigenciaRequest request, CancellationToken ct);
    Task<Result<IEnumerable<PaisDto>>> GetPaisesAsync(CancellationToken ct);
}
