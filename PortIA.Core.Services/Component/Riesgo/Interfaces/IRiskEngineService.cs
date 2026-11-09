namespace PortIA.Core.Services.Component.Riesgo.Interfaces;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Common.Results;

public interface IRiskEngineService
{
    Task<Result> RecalcularAsync(int containerId, CancellationToken ct);

    Task RecalcularAsync(Container container, CancellationToken ct);
}
