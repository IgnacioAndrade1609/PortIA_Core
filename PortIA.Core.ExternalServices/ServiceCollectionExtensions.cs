namespace PortIA.Core.ExternalServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using PortIA.Core.ExternalServices.Component.Storage.Clients;
using PortIA.Core.ExternalServices.Component.Storage.Interfaces;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMinio(configureClient => configureClient
            .WithEndpoint(configuration["Minio:Endpoint"])
            .WithCredentials(
                configuration["Minio:AccessKey"],
                configuration["Minio:SecretKey"])
            .WithSSL(bool.Parse(configuration["Minio:WithSSL"] ?? "false"))
            .Build());

        services.AddScoped<IMinioStorageClient, MinioStorageClient>();

        return services;
    }
}
