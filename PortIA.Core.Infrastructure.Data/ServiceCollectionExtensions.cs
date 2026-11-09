using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnitOfWorkNs = PortIA.Core.Infrastructure.Data.UnitOfWork;

namespace PortIA.Core.Infrastructure.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<PortIACoreDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<UnitOfWorkNs.IUnitOfWork, UnitOfWorkNs.UnitOfWork>();

        return services;
    }
}
