namespace PortIA.Core.Services.Component.Storage.Services;

using Minio;
using Minio.DataModel.Args;
using PortIA.Core.Services.Component.Storage.Interfaces;

public class MinioStorageService(IMinioClient minioClient) : IMinioStorageService
{
    private const string BucketName = "portia-documentos";

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct)
    {
        var beArgs = new BucketExistsArgs().WithBucket(BucketName);
        var found = await minioClient.BucketExistsAsync(beArgs, ct);
        
        if (!found)
        {
            var mbArgs = new MakeBucketArgs().WithBucket(BucketName);
            await minioClient.MakeBucketAsync(mbArgs, ct);
        }

        var objectName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await minioClient.PutObjectAsync(putObjectArgs, ct);

        return $"{BucketName}/{objectName}";
    }

    public async Task RemoveFileAsync(string storagePath, CancellationToken ct)
    {
        try
        {
            var parts = storagePath.Split('/', 2);
            if (parts.Length < 2) return;

            var bucket = parts[0];
            var objectName = parts[1];

            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName);

            await minioClient.RemoveObjectAsync(removeArgs, ct);
        }
        catch
        {
            // Silencia excepciones de limpieza
        }
    }
}