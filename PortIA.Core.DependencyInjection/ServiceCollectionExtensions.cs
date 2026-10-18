namespace PortIA.Core.DependencyInjection;

using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Services;
using PortIA.Core.Services.Component.Containers.Interfaces;
using PortIA.Core.Services.Component.Containers.Services;
using PortIA.Core.Services.Component.Mensajeria.Consumers;
using PortIA.Core.Services.Component.Mensajeria.Events;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAll(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices(configuration);
        services.AddInfrastructureData(configuration);
        services.AddMessaging(configuration);
        services.AddContainers();
        return services;
    }

    private static IServiceCollection AddContainers(this IServiceCollection services)
    {
        services.AddScoped<IContainerService, ContainerService>();
        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqUrl = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("Connection string 'RabbitMq' not found.");

        services.AddMassTransit(x =>
        {
            x.AddConsumer<DataExtractedConsumer>();
            x.AddConsumer<RealStatusUpdatedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(new Uri(rabbitMqUrl));

                cfg.ConfigureJsonSerializerOptions(options =>
                {
                    options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
                    return options;
                });

                cfg.UseRawJsonSerializer();

                cfg.Message<InspectContainer>(m => m.SetEntityName("inspect_container"));
                cfg.Publish<InspectContainer>(p => p.ExchangeType = "fanout");

                cfg.ConfigureEndpoints(ctx, SnakeCaseEndpointNameFormatter.Instance);
            });
        });

        return services;
    }
}