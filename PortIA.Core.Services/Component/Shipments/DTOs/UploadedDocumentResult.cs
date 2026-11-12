namespace PortIA.Core.Services.Component.Shipments.DTOs;

public record UploadedDocumentResult(
    string FileName,
    string Status,
    int? DocumentId,
    string? Reason
);
