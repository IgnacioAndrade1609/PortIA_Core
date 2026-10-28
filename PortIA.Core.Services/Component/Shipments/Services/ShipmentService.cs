namespace PortIA.Core.Services.Component.Shipments.Services;

using System.Security.Cryptography;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Interfaces;
using PortIA.Core.Services.Component.Shipments.Mappers;
using PortIA.Core.ExternalServices.Component.Storage.Interfaces;

public class ShipmentService(
    IUnitOfWork unitOfWork,
    IMinioStorageClient minioService,
    IValidator<CreateShipmentRequest> createShipmentValidator,
    ILogger<ShipmentService> logger) : IShipmentService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<ShipmentCreatedResponse>> CreateShipmentAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        int userId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var validationError = await ValidateCreateRequestAsync(fileName, contentType, fileSize, ct);
        if (validationError is not null)
            return validationError;

        var documentHash = await ComputeFileHashAsync(fileStream, ct);

        var duplicateError = await EnsureDocumentIsNotDuplicateAsync(documentHash, ct);
        if (duplicateError is not null)
            return duplicateError;

        var storagePath = await minioService.UploadFileAsync(fileStream, fileName, contentType, ct);

        return await PersistShipmentAsync(storagePath, documentHash, userId, ct);
    }

    public async Task<Result<PagedResult<ShipmentSummaryResponse>>> GetShipmentsAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        string? sort,
        CancellationToken ct)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);

        var shipmentsQuery = ApplySort(ApplyStatusFilter(
            unitOfWork.CargasDocumentos.Query().Where(carga => carga.UserId == userId), status), sort);

        var totalItems = await shipmentsQuery.LongCountAsync(ct);
        var shipmentSummaries = await FetchSummaryPageAsync(shipmentsQuery, page, pageSize, ct);

        return Result<PagedResult<ShipmentSummaryResponse>>.Success(new PagedResult<ShipmentSummaryResponse>(
            Items: shipmentSummaries,
            Page: page,
            PageSize: pageSize,
            TotalItems: totalItems,
            TotalPages: CalculateTotalPages(totalItems, pageSize)
        ));
    }

    public async Task<Result<ShipmentDetailResponse>> GetShipmentByIdAsync(int id, int userId, CancellationToken ct)
    {
        var carga = await unitOfWork.CargasDocumentos.GetByIdForUserAsync(id, userId, ct);

        if (carga is null)
            return Result<ShipmentDetailResponse>.NotFound("La carga no existe o no pertenece al usuario.");

        var documentDetails = await FetchDocumentDetailsAsync(id, ct);

        return Result<ShipmentDetailResponse>.Success(carga.ToDetailResponse(documentDetails));
    }


    private async Task<Result<ShipmentCreatedResponse>?> ValidateCreateRequestAsync(
        string fileName, string contentType, long fileSize, CancellationToken ct)
    {
        var validation = await createShipmentValidator.ValidateAsync(
            new CreateShipmentRequest(fileName, contentType, fileSize), ct);

        return validation.IsValid
            ? null
            : Result<ShipmentCreatedResponse>.ValidationError(validation.ToDictionary());
    }

    private static async Task<string> ComputeFileHashAsync(Stream fileStream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream, ct);
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        return hash;
    }

    private async Task<Result<ShipmentCreatedResponse>?> EnsureDocumentIsNotDuplicateAsync(string documentHash, CancellationToken ct)
    {
        var existingDocumentWithSameHash = await unitOfWork.Documentos.GetByHashAsync(documentHash, ct);

        if (existingDocumentWithSameHash is null)
            return null;

        logger.LogWarning(
            "CreateShipment rechazado: documento duplicado (hash={DocumentHash}, cargaExistente={CargaId})",
            documentHash, existingDocumentWithSameHash.IdCargaDocumentos);

        return Result<ShipmentCreatedResponse>.BusinessError(
            $"El documento ya fue subido previamente en la carga #{existingDocumentWithSameHash.IdCargaDocumentos}.");
    }

    private async Task<Result<ShipmentCreatedResponse>> PersistShipmentAsync(
        string storagePath, string documentHash, int userId, CancellationToken ct)
    {
        try
        {
            var cargaDocumentos = BuildCargaDocumentos(userId);
            await unitOfWork.CargasDocumentos.AddAsync(cargaDocumentos, ct);
            await unitOfWork.SaveChangesAsync(ct);

            var documento = BuildDocumento(cargaDocumentos.Id, storagePath, documentHash);
            await unitOfWork.Documentos.AddAsync(documento, ct);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Shipment creado (cargaId={CargaId}, documentoId={DocumentoId}, userId={UserId})",
                cargaDocumentos.Id, documento.Id, userId);

            return Result<ShipmentCreatedResponse>.Success(cargaDocumentos.ToCreatedResponse(documento));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Fallo al persistir shipment (userId={UserId}, storagePath={StoragePath}); se revierte el archivo subido",
                userId, storagePath);
            await minioService.RemoveFileAsync(storagePath, ct);
            throw;
        }
    }

    private static CargaDocumentos BuildCargaDocumentos(int userId) => new()
    {
        UserId = userId,
        Status = "PROCESANDO",
        CreacionFecha = DateTime.UtcNow,
        UpdateAt = DateTime.UtcNow,
        Vigente = true
    };

    private static Documento BuildDocumento(int cargaDocumentosId, string storagePath, string documentHash) => new()
    {
        IdCargaDocumentos = cargaDocumentosId,
        TipoDocumento = "BL",
        StorageUrl = storagePath,
        HuellaHash = documentHash,
        Confidence = null,
        EstadoExtraccion = "PENDIENTE",
        ContieneDatosPersonales = true,
        CreateAt = DateTime.UtcNow,
        Vigente = true
    };


    private static int NormalizePage(int page) => page < 1 ? 1 : page;

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => pageSize
    };

    private static IQueryable<CargaDocumentos> ApplyStatusFilter(IQueryable<CargaDocumentos> query, string? status) =>
        string.IsNullOrWhiteSpace(status) ? query : query.Where(carga => carga.Status == status.ToUpper());

    private static IQueryable<CargaDocumentos> ApplySort(IQueryable<CargaDocumentos> query, string? sort) => sort switch
    {
        "createdAt" => query.OrderBy(carga => carga.CreacionFecha),
        "-createdAt" or null or "" => query.OrderByDescending(carga => carga.CreacionFecha),
        "status" => query.OrderBy(carga => carga.Status),
        "-status" => query.OrderByDescending(carga => carga.Status),
        _ => query.OrderByDescending(carga => carga.CreacionFecha)
    };

    private static int CalculateTotalPages(long totalItems, int pageSize) =>
        (int)Math.Ceiling(totalItems / (double)pageSize);

    private async Task<List<ShipmentSummaryResponse>> FetchSummaryPageAsync(
        IQueryable<CargaDocumentos> query, int page, int pageSize, CancellationToken ct)
    {
        var documentosQuery = unitOfWork.Documentos.Query();
        var containersQuery = unitOfWork.Containers.Query();

        var summaryProjections = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(carga => new ShipmentSummaryProjection(
                carga.Id,
                carga.Status,
                documentosQuery.Count(documento => documento.IdCargaDocumentos == carga.Id),
                containersQuery.Count(container => container.IdCargaDocumentos == carga.Id),
                carga.CreacionFecha,
                carga.UpdateAt
            ))
            .ToListAsync(ct);

        return summaryProjections.Select(projection => projection.ToSummaryResponse()).ToList();
    }


    private async Task<List<DocumentDetailResponse>> FetchDocumentDetailsAsync(int cargaDocumentosId, CancellationToken ct)
    {
        var documentProjections = await unitOfWork.Documentos.Query()
            .Where(documento => documento.IdCargaDocumentos == cargaDocumentosId)
            .Select(documento => new DocumentDetailProjection(
                documento.Id,
                documento.TipoDocumento,
                documento.StorageUrl,
                documento.Confidence,
                documento.EstadoExtraccion,
                documento.ContieneDatosPersonales,
                documento.RetencionHasta
            ))
            .ToListAsync(ct);

        return documentProjections.Select(projection => projection.ToDetailResponse()).ToList();
    }
}
