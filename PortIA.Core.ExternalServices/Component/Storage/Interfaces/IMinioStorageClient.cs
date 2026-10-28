namespace PortIA.Core.ExternalServices.Component.Storage.Interfaces;

public interface IMinioStorageClient
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct);
    Task RemoveFileAsync(string storagePath, CancellationToken ct);
}
