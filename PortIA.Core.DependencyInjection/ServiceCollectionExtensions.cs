using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Services;
using PortIA.Core.Services.Component.Mensajeria.Consumers;

namespace PortIA.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAll(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices(configuration);
        services.AddInfrastructureData(configuration);
        services.AddMessaging(configuration);
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

                cfg.ConfigureEndpoints(ctx, SnakeCaseEndpointNameFormatter.Instance);
            });
        });

        return services;
    }
}
