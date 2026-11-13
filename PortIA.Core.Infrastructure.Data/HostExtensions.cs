using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PortIA.Core.Infrastructure.Data.Entities;

namespace PortIA.Core.Infrastructure.Data;

public static class HostExtensions
{
    public static IHost ApplyPendingMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortIACoreDbContext>();
        dbContext.Database.Migrate();
        EnsureTerminalesSeed(dbContext);

        return host;
    }

    /// <summary>
    /// Datos mínimos para que el flujo de agentes pueda asignar terminal de llegada
    /// (sin terminal el Core no publica <c>InspectContainer</c>). Idempotente: solo
    /// inserta si la tabla está vacía.
    /// </summary>
    private static void EnsureTerminalesSeed(PortIACoreDbContext dbContext)
    {
        if (dbContext.Terminales.Any())
        {
            return;
        }

        dbContext.Terminales.AddRange(
            new Terminal { CodigoTerminal = "STI", NombreTerminal = "San Antonio Terminal Internacional", IdPais = 1, ConfigScraper = "https://www.stiport.com/", Vigente = true },
            new Terminal { CodigoTerminal = "DPW", NombreTerminal = "DP World San Antonio", IdPais = 1, ConfigScraper = "https://www.dpworld.com/", Vigente = true },
            new Terminal { CodigoTerminal = "TPS", NombreTerminal = "Terminal Pacífico Sur Valparaíso", IdPais = 1, ConfigScraper = "https://www.tps.cl/", Vigente = true },
            new Terminal { CodigoTerminal = "TPA", NombreTerminal = "Terminal Portuario Arica", IdPais = 1, ConfigScraper = "https://www.puertoarica.cl/", Vigente = true },
            new Terminal { CodigoTerminal = "ITI", NombreTerminal = "Iquique Terminal Internacional", IdPais = 1, ConfigScraper = "https://www.iti.cl/", Vigente = true },
            new Terminal { CodigoTerminal = "ATI", NombreTerminal = "Antofagasta Terminal Internacional", IdPais = 1, ConfigScraper = "https://www.ati.cl/", Vigente = true });

        dbContext.SaveChanges();
    }
}
