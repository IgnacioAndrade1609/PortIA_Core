namespace PortIA.Core.Services.Component.Shipments.Interfaces;

using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Shipments.DTOs;

public interface IDocumentReviewService
{
    Task<Result<List<PendingDocumentResponse>>> GetPendientesAsync(int userId, CancellationToken ct);

    Task<Result<ValidateDocumentResponse>> ValidarAsync(int documentId, int userId, ValidateDocumentRequest request, CancellationToken ct);
}
