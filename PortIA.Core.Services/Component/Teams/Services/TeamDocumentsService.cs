namespace PortIA.Core.Services.Component.Teams.Services;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PortIA.Core.ExternalServices.Component.Storage.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Teams.DTOs;
using PortIA.Core.Services.Component.Teams.Interfaces;

public class TeamDocumentsService(
    IUnitOfWork unitOfWork,
    IMinioStorageClient minioStorageClient,
    IValidator<TeamDocumentsFilter> validator) : ITeamDocumentsService
{
    public async Task<Result<List<AnalystDocumentResponse>>> GetDocumentsAsync(
        int jefeId, int callerUserId, string callerRole, TeamDocumentsFilter filter, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(filter, ct);
        if (!validation.IsValid)
            return Result<List<AnalystDocumentResponse>>.ValidationError(validation.ToDictionary());

        var normalizedRole = callerRole.ToUpperInvariant();
        int effectiveJefeId;
        if (normalizedRole is "JEFE" or "BOSS")
        {
            effectiveJefeId = callerUserId;
        }
        else if (normalizedRole == "ADMIN")
        {
            effectiveJefeId = jefeId;
        }
        else
        {
            return Result<List<AnalystDocumentResponse>>.Unauthorized("No tiene permisos para ver estos documentos.");
        }

        var jefe = await unitOfWork.Usuarios.GetByIdWithRoleAsync(effectiveJefeId, ct);
        if (jefe is null)
            return Result<List<AnalystDocumentResponse>>.NotFound($"No se encontró el jefe con ID {effectiveJefeId}.");

        if (!jefe.Rol.NombreRol.Equals("Jefe", StringComparison.OrdinalIgnoreCase))
            return Result<List<AnalystDocumentResponse>>.BusinessError("El usuario indicado no tiene rol Jefe.");

        return await BuildDocumentsAsync(effectiveJefeId, filter, ct);
    }

    public async Task<Result<List<AnalystDocumentResponse>>> GetAllDocumentsAsync(
        string callerRole, TeamDocumentsFilter filter, CancellationToken ct)
    {
        if (!callerRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return Result<List<AnalystDocumentResponse>>.Unauthorized("Solo un administrador puede ver los documentos de todos los equipos.");

        var validation = await validator.ValidateAsync(filter, ct);
        if (!validation.IsValid)
            return Result<List<AnalystDocumentResponse>>.ValidationError(validation.ToDictionary());

        return await BuildDocumentsAsync(null, filter, ct);
    }

    private async Task<Result<List<AnalystDocumentResponse>>> BuildDocumentsAsync(
        int? jefeId, TeamDocumentsFilter filter, CancellationToken ct)
    {
        var query = unitOfWork.Documentos.Query()
            .Where(d => jefeId == null ? d.CargaDocumentos.Usuario.JefeId != null : d.CargaDocumentos.Usuario.JefeId == jefeId);

        if (filter.AnalystId.HasValue)
            query = query.Where(d => d.CargaDocumentos.UserId == filter.AnalystId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(d => d.CreateAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(d => d.CreateAt <= filter.ToDate.Value);

        var documentos = await query
            .OrderByDescending(d => d.CreateAt)
            .Select(d => new
            {
                d.Id,
                d.TipoDocumento,
                d.CreateAt,
                d.EstadoExtraccion,
                d.Confidence,
                d.StorageUrl,
                AnalystId = d.CargaDocumentos.UserId,
                AnalystName = d.CargaDocumentos.Usuario.NombreUsuario,
                Container = d.CargaDocumentos.Containers.Select(c => new { c.Id, c.NumeroContainer }).FirstOrDefault()
            })
            .ToListAsync(ct);

        var respuesta = new List<AnalystDocumentResponse>();
        foreach (var d in documentos)
        {
            var downloadUrl = await minioStorageClient.GetPresignedUrlAsync(d.StorageUrl, 900, ct);
            respuesta.Add(new AnalystDocumentResponse(
                d.Id,
                d.TipoDocumento,
                d.CreateAt,
                d.EstadoExtraccion,
                d.Confidence,
                d.AnalystId,
                d.AnalystName,
                d.Container?.Id,
                d.Container?.NumeroContainer,
                downloadUrl));
        }

        return Result<List<AnalystDocumentResponse>>.Success(respuesta);
    }
}
