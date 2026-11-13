namespace PortIA.Core.Services.Component.Shipments.Services;

using System.Security.Cryptography;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Interfaces;
using PortIA.Core.Services.Component.Shipments.Mappers;
using PortIA.Core.ExternalServices.Component.Storage.Interfaces;

public class ShipmentService(
    IUnitOfWork unitOfWork,
    IMinioStorageClient minioService,
    IValidator<CreateShipmentRequest> createShipmentValidator,
    IPublishEndpoint publishEndpoint,
    ILogger<ShipmentService> logger) : IShipmentService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<ShipmentCreatedResponse>> CreateShipmentAsync(
        IReadOnlyList<ShipmentFileInput> files,
        int userId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (files.Count == 0)
        {
            return Result<ShipmentCreatedResponse>.ValidationError(new Dictionary<string, string[]>
            {
                ["files"] = ["Debe adjuntar al menos un archivo."],
            });
        }

        var evaluaciones = await EvaluarArchivosAsync(files, ct);
        var archivosValidos = evaluaciones.Where(e => e.EsValido).ToList();

        if (archivosValidos.Count == 0)
        {
            var errores = evaluaciones
                .Select((e, i) => (Key: $"files[{i}]", Value: new[] { $"{e.Input.FileName}: {e.Motivo}" }))
                .ToDictionary(x => x.Key, x => x.Value);
            return Result<ShipmentCreatedResponse>.ValidationError(errores);
        }

        var persistResult = await PersistShipmentAsync(storagePath, documentHash, userId, ct);
        if (!persistResult.IsSuccess)
            return persistResult;

        await PublishShipmentCreatedAsync(storagePath, persistResult.Value, ct);

        return persistResult;
    }

    private async Task PublishShipmentCreatedAsync(
        string storagePath,
        ShipmentCreatedResponse shipment,
        CancellationToken ct)
    {
        try
        {
            // El agente corre fuera de la red de MinIO: necesita una URL firmada, no el
            // storagePath interno ("bucket/objeto").
            var documentUrl = await minioService.GetPresignedUrlAsync(storagePath, 1800, ct);

            await publishEndpoint.Publish(
                new ShipmentCreated(
                    shipment.ShipmentId,
                    shipment.DocumentId,
                    documentUrl,
                    "BL"),
                ct);

            logger.LogInformation(
                "ShipmentCreated publicado (shipmentId={ShipmentId}, documentId={DocumentId})",
                shipment.ShipmentId,
                shipment.DocumentId);
        }
        catch (Exception ex)
        {
            // El PDF ya quedó persistido: no rompemos la subida si RabbitMQ falla.
            logger.LogError(ex,
                "No se pudo publicar ShipmentCreated (shipmentId={ShipmentId}, documentId={DocumentId})",
                shipment.ShipmentId,
                shipment.DocumentId);
        }
        return await PersistShipmentAsync(evaluaciones, userId, ct);
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


    private async Task<List<EvaluacionArchivo>> EvaluarArchivosAsync(IReadOnlyList<ShipmentFileInput> files, CancellationToken ct)
    {
        var evaluaciones = new List<EvaluacionArchivo>();
        var hashesEnLote = new HashSet<string>();

        foreach (var file in files)
        {
            var motivoValidacion = await ValidarArchivoAsync(file, ct);
            if (motivoValidacion is not null)
            {
                evaluaciones.Add(new EvaluacionArchivo(file, false, motivoValidacion, null, EsDuplicado: false));
                continue;
            }

            var hash = await ComputeFileHashAsync(file.Content, ct);

            if (!hashesEnLote.Add(hash))
            {
                evaluaciones.Add(new EvaluacionArchivo(file, false, "Archivo duplicado dentro de esta misma carga.", hash, EsDuplicado: true));
                continue;
            }

            var existente = await unitOfWork.Documentos.GetByHashAsync(hash, ct);
            if (existente is not null)
            {
                evaluaciones.Add(new EvaluacionArchivo(
                    file, false, $"El documento ya fue subido previamente en la carga #{existente.IdCargaDocumentos}.", hash, EsDuplicado: true));
                continue;
            }

            evaluaciones.Add(new EvaluacionArchivo(file, true, null, hash, EsDuplicado: false));
        }

        return evaluaciones;
    }

    private async Task<string?> ValidarArchivoAsync(ShipmentFileInput file, CancellationToken ct)
    {
        var validation = await createShipmentValidator.ValidateAsync(
            new CreateShipmentRequest(file.FileName, file.ContentType, file.Length), ct);

        return validation.IsValid
            ? null
            : string.Join(" ", validation.Errors.Select(e => e.ErrorMessage));
    }

    private sealed record EvaluacionArchivo(ShipmentFileInput Input, bool EsValido, string? Motivo, string? Hash, bool EsDuplicado);

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

    private async Task<Result<ShipmentCreatedResponse>> PersistShipmentAsync(
        List<EvaluacionArchivo> evaluaciones, int userId, CancellationToken ct)
    {
        var archivosValidos = evaluaciones.Where(e => e.EsValido).ToList();
        var storagePathsSubidos = new List<string>();

        try
        {
            var cargaDocumentos = BuildCargaDocumentos(userId);
            await unitOfWork.CargasDocumentos.AddAsync(cargaDocumentos, ct);
            await unitOfWork.SaveChangesAsync(ct);

            var documentosPorEvaluacion = new Dictionary<EvaluacionArchivo, Documento>();

            foreach (var evaluacion in archivosValidos)
            {
                var storagePath = await minioService.UploadFileAsync(
                    evaluacion.Input.Content, evaluacion.Input.FileName, evaluacion.Input.ContentType, ct);
                storagePathsSubidos.Add(storagePath);

                var documento = BuildDocumento(cargaDocumentos.Id, storagePath, evaluacion.Hash!);
                await unitOfWork.Documentos.AddAsync(documento, ct);
                documentosPorEvaluacion[evaluacion] = documento;
            }

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Shipment creado (cargaId={CargaId}, documentosCreados={DocumentCount}, documentosRechazados={RejectedCount}, userId={UserId})",
                cargaDocumentos.Id, documentosPorEvaluacion.Count, evaluaciones.Count - archivosValidos.Count, userId);

            var resultadosPorArchivo = evaluaciones
                .Select(e => documentosPorEvaluacion.TryGetValue(e, out var documento)
                    ? new UploadedDocumentResult(e.Input.FileName, "PENDIENTE", documento.Id, null)
                    : new UploadedDocumentResult(e.Input.FileName, e.EsDuplicado ? "DUPLICADO" : "RECHAZADO", null, e.Motivo))
                .ToList();

            return Result<ShipmentCreatedResponse>.Success(cargaDocumentos.ToCreatedResponse(resultadosPorArchivo));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Fallo al persistir shipment (userId={UserId}, archivosSubidos={Count}); se revierten los archivos subidos",
                userId, storagePathsSubidos.Count);
            foreach (var storagePath in storagePathsSubidos)
            {
                await minioService.RemoveFileAsync(storagePath, ct);
            }
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
