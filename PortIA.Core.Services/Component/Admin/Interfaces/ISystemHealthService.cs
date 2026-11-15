namespace PortIA.Core.Services.Component.Admin.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Admin.DTOs;

public interface ISystemHealthService
{
    Task<Result<SystemHealthResponse>> GetAsync(CancellationToken ct);
}
