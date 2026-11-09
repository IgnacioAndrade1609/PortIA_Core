namespace PortIA.Core.Services.Component.Riesgo.BackgroundServices;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Riesgo.Interfaces;

public class RiskEngineBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<RiskEngineBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RecalcularTodosAsync(stoppingToken);

            try
            {
                await Task.Delay(Intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RecalcularTodosAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var riskEngine = scope.ServiceProvider.GetRequiredService<IRiskEngineService>();

        var containerIds = await unitOfWork.Containers.Query()
            .Where(c => c.FechaArriboReal != null)
            .Select(c => c.Id)
            .ToListAsync(ct);

        foreach (var containerId in containerIds)
        {
            try
            {
                await riskEngine.RecalcularAsync(containerId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RiskEngineBackgroundService: fallo al recalcular container_id={ContainerId}", containerId);
            }
        }

        logger.LogInformation("RiskEngineBackgroundService: recálculo diario completado para {Count} contenedores", containerIds.Count);
    }
}
