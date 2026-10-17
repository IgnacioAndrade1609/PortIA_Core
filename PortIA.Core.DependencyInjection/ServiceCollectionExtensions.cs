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
        services.AddServices();
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

                // Los eventos viajan en snake_case (contrato EVENTOS.md); las clases C#
                // usan PascalCase. Esta política los traduce automáticamente al serializar.
                cfg.ConfigureJsonSerializerOptions(options =>
                {
                    options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
                    return options;
                });

                // DataExtracted/RealStatusUpdated: el agente Python los publica directo a una
                // cola nombrada (snake_case) vía el exchange por defecto de RabbitMQ
                // (routing_key = nombre de cola, sin exchange propio). Por eso se fuerza acá
                // el mismo formato de nombre ("data_extracted", "real_status_updated") en vez
                // de dejar el default de MassTransit (kebab-case) — si no, el agente nunca
                // encuentra la cola porque el nombre no calza.
                cfg.ConfigureEndpoints(ctx, SnakeCaseEndpointNameFormatter.Instance);
            });
        });

        return services;
    }
}
