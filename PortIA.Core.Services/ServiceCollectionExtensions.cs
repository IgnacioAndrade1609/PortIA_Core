using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PortIA.Core.Services.Common.Security;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.Services.Component.Alerts.Services;
using PortIA.Core.Services.Component.Audit.Interfaces;
using PortIA.Core.Services.Component.Audit.Services;
using PortIA.Core.Services.Component.Auth.Interfaces;
using PortIA.Core.Services.Component.Auth.Services;
using PortIA.Core.Services.Component.Containers.Interfaces;
using PortIA.Core.Services.Component.Containers.Services;
using PortIA.Core.Services.Component.Mensajeria.Publishers;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;
using PortIA.Core.Services.Component.Navieras.Interfaces;
using PortIA.Core.Services.Component.Navieras.Services;
using PortIA.Core.Services.Component.Admin.Interfaces;
using PortIA.Core.Services.Component.Admin.Services;
using PortIA.Core.Services.Component.Notifications.Interfaces;
using PortIA.Core.Services.Component.Notifications.Services;
using PortIA.Core.Services.Component.Riesgo.BackgroundServices;
using PortIA.Core.Services.Component.Riesgo.Interfaces;
using PortIA.Core.Services.Component.Riesgo.Services;
using PortIA.Core.Services.Component.Shipments.Interfaces;
using PortIA.Core.Services.Component.Shipments.Services;
using PortIA.Core.Services.Component.Terminales.Interfaces;
using PortIA.Core.Services.Component.Terminales.Services;
using PortIA.Core.Services.Component.Teams.Interfaces;
using PortIA.Core.Services.Component.Teams.Services;
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
        services.AddScoped<INavieraService, NavieraService>();
        services.AddScoped<ITerminalService, TerminalService>();
        services.AddScoped<ITeamDashboardService, TeamDashboardService>();
        services.AddScoped<ITeamDocumentsService, TeamDocumentsService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<ISystemHealthService, SystemHealthService>();

        // Componente Notifications
        services.AddScoped<ITelegramSender, TelegramSender>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddValidatorsFromAssemblyContaining<ServicesAssemblyMarker>(ServiceLifetime.Scoped);

        return services;
    }
}