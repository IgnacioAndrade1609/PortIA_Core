namespace PortIA.Core.Services.Component.Teams.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Teams.DTOs;

public interface ITeamDocumentsService
{
    Task<Result<List<AnalystDocumentResponse>>> GetDocumentsAsync(
        int jefeId, int callerUserId, string callerRole, TeamDocumentsFilter filter, CancellationToken ct);
}
