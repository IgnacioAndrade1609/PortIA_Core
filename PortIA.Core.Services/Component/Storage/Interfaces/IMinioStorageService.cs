namespace PortIA.Core.Services.Component.Storage.Interfaces;

public interface IMinioStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct);
    Task RemoveFileAsync(string storagePath, CancellationToken ct);
}