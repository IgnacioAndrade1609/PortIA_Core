namespace PortIA.Core.Services.Component.Auth.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Auth.DTOs;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
}
