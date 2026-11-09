namespace PortIA.Core.Services.Component.Shipments.Services;

using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Interfaces;

public class DocumentReviewService(
    IUnitOfWork unitOfWork,
    IInspectContainerPublisher inspectContainerPublisher,
    IValidator<ValidateDocumentRequest> validator,
    ILogger<DocumentReviewService> logger) : IDocumentReviewService
{
    private const string EstadoPendienteRevision = "PENDIENTE_REVISION";
    private const string EstadoValidado = "VALIDADO";
    private const string AccionActualizar = "ACTUALIZAR";
    private static readonly string[] PrefijosPuerto = ["Puerto de ", "Puerto "];

    public async Task<Result<List<PendingDocumentResponse>>> GetPendientesAsync(int userId, CancellationToken ct)
    {
        var pendientes = await unitOfWork.Documentos.Query()
            .Where(d => d.EstadoExtraccion == EstadoPendienteRevision && d.CargaDocumentos.UserId == userId)
            .OrderBy(d => d.CreateAt)
            .ToListAsync(ct);

        var respuesta = pendientes.Select(ToPendingResponse).ToList();

        return Result<List<PendingDocumentResponse>>.Success(respuesta);
    }

    public async Task<Result<ValidateDocumentResponse>> ValidarAsync(int documentId, int userId, ValidateDocumentRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<ValidateDocumentResponse>.ValidationError(validation.ToDictionary());
        }

        var documento = await unitOfWork.Documentos.GetByIdAsync(documentId, ct);
        if (documento is null)
        {
            return Result<ValidateDocumentResponse>.NotFound($"El documento {documentId} no existe.");
        }

        var carga = await unitOfWork.CargasDocumentos.GetByIdForUserAsync(documento.IdCargaDocumentos, userId, ct);
        if (carga is null)
        {
            logger.LogWarning(
                "ValidarDocumento rechazado: documento {DocumentId} no existe o no pertenece a userId={UserId}",
                documentId, userId);
            return Result<ValidateDocumentResponse>.NotFound($"El documento {documentId} no existe o no pertenece al usuario.");
        }

        if (documento.EstadoExtraccion != EstadoPendienteRevision)
        {
            return Result<ValidateDocumentResponse>.BusinessError("El documento no está pendiente de revisión.");
        }

        var yaRegistrado = await unitOfWork.Containers.Query()
            .AnyAsync(c => c.NumeroContainer == request.ContainerNumber, ct);
        if (yaRegistrado)
        {
            return Result<ValidateDocumentResponse>.Conflict(
                $"El contenedor '{request.ContainerNumber}' ya está registrado en otro envío.");
        }

        var terminales = await unitOfWork.Terminales.Query().ToListAsync(ct);
        var idTerminalLlegada = ResolveTerminalLlegadaId(request.PortOfDestination, terminales);

        var container = new Container
        {
            IdCargaDocumentos = documento.IdCargaDocumentos,
            NumeroContainer = request.ContainerNumber,
            PuertoDestino = request.PortOfDestination,
            NumeroBl = request.BlNumber,
            NombreForwarder = request.ForwarderName,
            FechaLlegadaEstimada = request.Eta,
            DiasLibresDemurrage = request.DemurrageFreeDays ?? 0,
            DiasLibresDetention = request.DetentionFreeDays ?? 0,
            IdTerminalLlegada = idTerminalLlegada,
        };

        await unitOfWork.Containers.AddAsync(container, ct);

        var valorOriginalIA = documento.DatosExtraidosRaw;
        documento.EstadoExtraccion = EstadoValidado;

        await unitOfWork.SaveChangesAsync(ct);

        await unitOfWork.Hitos.AddAsync(new Hito
        {
            IdContainer = container.Id,
            Descripcion = "Datos extraídos corregidos y validados manualmente por el analista.",
            Origen = "USUARIO",
            CreadoPor = userId,
            FechaOcurrencia = DateTimeOffset.UtcNow,
        }, ct);

        await RegistrarAuditoriaAsync(documento.Id, userId, valorOriginalIA, request, ct);

        await unitOfWork.SaveChangesAsync(ct);

        var inspectionResult = await inspectContainerPublisher.PublishAsync(container.Id, ct);
        if (!inspectionResult.IsSuccess)
        {
            logger.LogInformation("No se disparó InspectContainer para container_id={ContainerId}: {Error}",
                container.Id, inspectionResult.Error);
        }

        return Result<ValidateDocumentResponse>.Success(new ValidateDocumentResponse(container.Id));
    }

    private async Task RegistrarAuditoriaAsync(int documentId, int userId, string? valorOriginalIA, ValidateDocumentRequest corregido, CancellationToken ct)
    {
        var accion = await unitOfWork.Acciones.GetByDescripcionAsync(AccionActualizar, ct);
        if (accion is null)
        {
            logger.LogWarning(
                "No se encontró la acción '{Accion}', no se registra auditoría para document_id={DocumentId}",
                AccionActualizar, documentId);
            return;
        }

        await unitOfWork.LogsAuditoria.AddAsync(new LogAuditoria
        {
            NombreEntidad = nameof(Documento),
            IdEntidad = documentId,
            IdAccion = accion.Id,
            Responsable = userId,
            ValoresAnteriores = valorOriginalIA,
            ValoresNuevos = JsonSerializer.Serialize(corregido),
        }, ct);
    }

    private static PendingDocumentResponse ToPendingResponse(Documento documento)
    {
        var snapshot = string.IsNullOrWhiteSpace(documento.DatosExtraidosRaw)
            ? null
            : JsonSerializer.Deserialize<ExtractedDataSnapshot>(documento.DatosExtraidosRaw);

        return new PendingDocumentResponse(
            documento.Id,
            documento.IdCargaDocumentos,
            documento.Confidence,
            snapshot?.ContainerNumber,
            snapshot?.BlNumber,
            snapshot?.ShippingLine,
            snapshot?.PortOfDestination,
            snapshot?.Eta,
            snapshot?.DemurrageFreeDays,
            snapshot?.DetentionFreeDays,
            snapshot?.ForwarderName,
            documento.CreateAt);
    }

    private static int? ResolveTerminalLlegadaId(string? puertoDestino, List<Terminal> terminales)
    {
        if (string.IsNullOrWhiteSpace(puertoDestino))
        {
            return null;
        }

        var ciudad = puertoDestino;
        foreach (var prefijo in PrefijosPuerto)
        {
            if (ciudad.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            {
                ciudad = ciudad[prefijo.Length..];
                break;
            }
        }
        ciudad = ciudad.Trim();

        var candidato = terminales.FirstOrDefault(t => t.NombreTerminal.Contains(ciudad, StringComparison.OrdinalIgnoreCase));
        return candidato?.Id;
    }
}
