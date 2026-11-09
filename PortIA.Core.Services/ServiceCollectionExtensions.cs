using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PortIA.Core.Services.Common.Security;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.Services.Component.Alerts.Services;
using PortIA.Core.Services.Component.Auth.Interfaces;
using PortIA.Core.Services.Component.Auth.Services;
using PortIA.Core.Services.Component.Containers.Interfaces;
using PortIA.Core.Services.Component.Containers.Services;
using PortIA.Core.Services.Component.Mensajeria.Publishers;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;
using PortIA.Core.Services.Component.Riesgo.BackgroundServices;
using PortIA.Core.Services.Component.Riesgo.Interfaces;
using PortIA.Core.Services.Component.Riesgo.Services;
using PortIA.Core.Services.Component.Shipments.Interfaces;
using PortIA.Core.Services.Component.Shipments.Services;
using PortIA.Core.Services.Component.Users.Interfaces;
using PortIA.Core.Services.Component.Users.Services;

namespace PortIA.Core.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddScoped<IDocumentReviewService, DocumentReviewService>();
        services.AddScoped<IContainerService, ContainerService>();
        services.AddScoped<IInspectContainerPublisher, InspectContainerPublisher>();

        services.AddScoped<IRiskEngineService, RiskEngineService>();
        services.AddHostedService<RiskEngineBackgroundService>();

        services.AddScoped<IAlertService, AlertService>();

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        services.AddValidatorsFromAssemblyContaining<ServicesAssemblyMarker>(ServiceLifetime.Scoped);

        return services;
    }
}
