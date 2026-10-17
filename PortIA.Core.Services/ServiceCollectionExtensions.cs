using Microsoft.Extensions.DependencyInjection;
using PortIA.Core.Services.Component.Mensajeria;
using PortIA.Core.Services.Component.Mensajeria.Interfaces;

namespace PortIA.Core.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IMensajeriaService, MensajeriaService>();
        return services;
    }
}
