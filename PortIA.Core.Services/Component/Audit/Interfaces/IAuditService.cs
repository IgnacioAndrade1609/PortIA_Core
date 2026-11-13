namespace PortIA.Core.Services.Component.Audit.Interfaces;

using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Audit.DTOs;

public interface IAuditService
{
    Task<Result<PagedResult<AuditLogResponse>>> GetPagedAsync(
        AuditQueryFilter filter, int callerUserId, string callerRole, CancellationToken ct);
}
