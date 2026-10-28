namespace PortIA.Core.Services.Component.Users.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Users.DTOs;

public interface IUsuarioService
{
    Task<Result<UsuarioResponse>> CreateAsync(CreateUsuarioRequest request, CancellationToken ct);
}
