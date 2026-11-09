namespace PortIA.Core.DependencyInjection;

using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortIA.Core.ExternalServices;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Services;
using PortIA.Core.Services.Component.Mensajeria.Configuration;
using PortIA.Core.Services.Component.Mensajeria.Consumers;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Riesgo.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAll(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices();
        services.AddInfrastructureData(configuration);
        services.AddExternalServices(configuration);
        services.AddMessaging(configuration);
        services.AddJwtAuthentication(configuration);
        services.Configure<RiskEngineOptions>(configuration.GetSection(RiskEngineOptions.SectionName));
        services.Configure<DiscrepanciaEtaOptions>(configuration.GetSection(DiscrepanciaEtaOptions.SectionName));
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