using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PortIA.Core.Infrastructure.Data;

public static class HostExtensions
{
    public static IHost ApplyPendingMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortIACoreDbContext>();
        dbContext.Database.Migrate();

        return host;
    }
}
