using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Services;

namespace PortIA.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAll(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices();
        services.AddInfrastructureData(configuration);
        return services;
    }
}
