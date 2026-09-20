using Microsoft.Extensions.Configuration;

namespace PortIA.Core.DependencyInjection;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddAppConfiguration(this IConfigurationBuilder builder)
    {
        return builder;
    }
}
