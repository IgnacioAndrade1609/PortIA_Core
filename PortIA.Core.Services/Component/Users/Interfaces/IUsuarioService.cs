namespace PortIA.Core.Services.Component.Users.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Users.DTOs;
using PortIA.Core.Services.Common.DTOs;

public interface IUsuarioService
{
    Task<Result<PagedResult<UserDto>>> GetPagedAsync(UserQueryFilter filter, CancellationToken ct);
    Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<UserDto>> CreateAsync(CreateUsuarioRequest request, CancellationToken ct);
    Task<Result<UserDto>> UpdateAsync(int id, UpdateUsuarioRequest request, CancellationToken ct);
    Task<Result<UserDto>> UpdateStatusAsync(int id, UpdateUsuarioStatusRequest request, int currentUserId, CancellationToken ct);
    Task<Result<UserDto>> UpdateRoleAsync(int id, UpdateUsuarioRoleRequest request, int currentUserId, CancellationToken ct);
    Task<Result<IEnumerable<RoleDto>>> GetRolesAsync(CancellationToken ct);
}