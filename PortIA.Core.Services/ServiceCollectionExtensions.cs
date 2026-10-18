using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using PortIA.Core.Services.Component.Shipments.Interfaces;
using PortIA.Core.Services.Component.Shipments.Services;
using PortIA.Core.Services.Component.Storage.Interfaces;
using PortIA.Core.Services.Component.Storage.Services;

namespace PortIA.Core.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMinio(configureClient => configureClient
            .WithEndpoint(configuration["Minio:Endpoint"])
            .WithCredentials(
                configuration["Minio:AccessKey"],
                configuration["Minio:SecretKey"])
            .WithSSL(bool.Parse(configuration["Minio:WithSSL"] ?? "false"))
            .Build());

        services.AddScoped<IMinioStorageService, MinioStorageService>();
        services.AddScoped<IShipmentService, ShipmentService>();

        services.AddValidatorsFromAssemblyContaining<ServicesAssemblyMarker>(ServiceLifetime.Scoped);

        return services;
    }
}
