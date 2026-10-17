using Microsoft.Extensions.Configuration;

namespace PortIA.Core.DependencyInjection;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddAppConfiguration(this IConfigurationBuilder builder)
    {
        var configureDir = Path.Combine(AppContext.BaseDirectory, "Configure");

        builder.AddJsonFile(Path.Combine(configureDir, "AppSettings.json"), optional: true, reloadOnChange: true);
        builder.AddJsonFile(Path.Combine(configureDir, "ConnectionStrings.json"), optional: true, reloadOnChange: true);
        builder.AddEnvironmentVariables();

        return builder;
    }
}
