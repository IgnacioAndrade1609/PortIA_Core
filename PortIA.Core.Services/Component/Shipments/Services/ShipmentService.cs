namespace PortIA.Core.Services.Component.Shipments.Services;

using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Common.DTOs;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Interfaces;
using PortIA.Core.Services.Component.Storage.Interfaces;

public class ShipmentService(
    PortIACoreDbContext dbContext,
    IMinioStorageService minioService) : IShipmentService
{
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<ShipmentCreatedResponseDto> CreateShipmentAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        int userId,
        CancellationToken ct)
    {
        if (fileStream is null || fileSize == 0)
            throw new ArgumentException("El archivo está vacío o es nulo.");

        if (fileSize > MaxFileSizeBytes)
            throw new ArgumentOutOfRangeException(nameof(fileSize), "El archivo supera el tamaño máximo permitido de 10 MB.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new NotSupportedException("Formato de archivo no permitido. Solo se aceptan PDF, PNG y JPG.");

        // 1. Cálculo del SHA-256 (Huella Hash)
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream, ct);
        var sha256Hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        // 2. Validación de duplicados en la tabla DOCUMENTO por HuellaHash
        var existingDoc = await dbContext.Documentos
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.HuellaHash == sha256Hash, ct);

        if (existingDoc is not null)
        {
            throw new InvalidOperationException(
                $"El documento ya fue subido previamente en la carga #{existingDoc.IdCargaDocumentos}.");
        }

        // 3. Subida del archivo físico a MinIO
        var storagePath = await minioService.UploadFileAsync(fileStream, fileName, contentType, ct);

        try
        {
            // 4. Inserción en la tabla CARGA_DOCUMENTOS
            var carga = new CargaDocumentos
            {
                UserId = userId,
                Status = "PROCESANDO",
                CreacionFecha = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                Vigente = true
            };
            dbContext.CargasDocumentos.Add(carga);
            await dbContext.SaveChangesAsync(ct);

            // 5. Inserción en la tabla DOCUMENTO
            var documento = new Documento
            {
                IdCargaDocumentos = carga.Id,
                TipoDocumento = "BL",
                StorageUrl = storagePath,
                HuellaHash = sha256Hash,
                Confidence = null,
                EstadoExtraccion = "PENDIENTE",
                ContieneDatosPersonales = true,
                CreateAt = DateTime.UtcNow,
                Vigente = true
            };
            dbContext.Documentos.Add(documento);
            await dbContext.SaveChangesAsync(ct);

            // 6. Retorno del DTO estandarizado
            return new ShipmentCreatedResponseDto(
                ShipmentId: carga.Id,
                DocumentId: documento.Id,
                Status: carga.Status,
                CreatedAt: carga.CreacionFecha.UtcDateTime
            );
        }
        catch (Exception)
        {
            // Compensación: Si la base de datos falla, eliminamos el archivo subido en MinIO
            await minioService.RemoveFileAsync(storagePath, ct);
            throw;
        }
    }

    public async Task<PagedResult<ShipmentSummaryDto>> GetShipmentsAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        string? sort,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var query = dbContext.CargasDocumentos
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Vigente);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status.ToUpper());
        }

        query = sort switch
        {
            "createdAt" => query.OrderBy(c => c.CreacionFecha),
            "-createdAt" or null or "" => query.OrderByDescending(c => c.CreacionFecha),
            "status" => query.OrderBy(c => c.Status),
            "-status" => query.OrderByDescending(c => c.Status),
            _ => query.OrderByDescending(c => c.CreacionFecha)
        };

        var totalItems = await query.LongCountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Se proyectan las propiedades primero para que la conversión a UtcDateTime ocurra en memoria sin romper LINQ to Entities
        var rawItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Status,
                DocumentCount = dbContext.Documentos.Count(d => d.IdCargaDocumentos == c.Id && d.Vigente),
                ContainerCount = dbContext.Containers.Count(co => co.IdCargaDocumentos == c.Id && co.Vigente),
                c.CreacionFecha,
                c.UpdateAt
            })
            .ToListAsync(ct);

        var items = rawItems.Select(c => new ShipmentSummaryDto(
            c.Id,
            c.Status,
            c.DocumentCount,
            c.ContainerCount,
            c.CreacionFecha.UtcDateTime,
            c.UpdateAt.UtcDateTime
        )).ToList();

        return new PagedResult<ShipmentSummaryDto>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalItems: totalItems,
            TotalPages: totalPages
        );
    }

    public async Task<ShipmentDetailDto?> GetShipmentByIdAsync(int id, int userId, CancellationToken ct)
    {
        var carga = await dbContext.CargasDocumentos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.Vigente, ct);

        if (carga is null) return null;

        var rawDocuments = await dbContext.Documentos
            .AsNoTracking()
            .Where(d => d.IdCargaDocumentos == id && d.Vigente)
            .Select(d => new
            {
                d.Id,
                d.TipoDocumento,
                d.StorageUrl,
                d.Confidence,
                d.EstadoExtraccion,
                d.ContieneDatosPersonales,
                d.RetencionHasta
            })
            .ToListAsync(ct);

        var documents = rawDocuments.Select(d => new DocumentDetailDto(
            d.Id,
            d.TipoDocumento,
            Path.GetFileName(d.StorageUrl),
            d.Confidence,
            d.Confidence.HasValue && d.Confidence.Value < 0.7f ? "NEEDS_REVIEW" : d.EstadoExtraccion,
            d.ContieneDatosPersonales,
            d.RetencionHasta.HasValue ? d.RetencionHasta.Value.UtcDateTime : null
        )).ToList();

        return new ShipmentDetailDto(
            Id: carga.Id,
            Status: carga.Status,
            Documents: documents,
            Containers: new List<object>()
        );
    }
}