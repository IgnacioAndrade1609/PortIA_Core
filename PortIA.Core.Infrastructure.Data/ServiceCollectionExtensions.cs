using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PortIA.Core.Infrastructure.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureData(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
