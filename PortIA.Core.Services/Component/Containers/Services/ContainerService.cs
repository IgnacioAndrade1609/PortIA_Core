namespace PortIA.Core.Services.Component.Containers.Services;

using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Containers.DTOs;
using PortIA.Core.Services.Component.Containers.Interfaces;
using PortIA.Core.Services.Component.Containers.Mappers;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;

public class ContainerService(
    IUnitOfWork unitOfWork,
    IInspectContainerPublisher inspectContainerPublisher) : IContainerService
{
    public Task<Result> TriggerInspectionAsync(int containerId, CancellationToken ct) =>
        inspectContainerPublisher.PublishAsync(containerId, ct);

    public async Task<Result<IReadOnlyList<HitoResponse>>> GetHitosAsync(int containerId, int userId, CancellationToken ct)
    {
        var container = await unitOfWork.Containers.GetByIdAsync(containerId, ct);

        if (container is null || container.AsignadoA != userId)
            return Result<IReadOnlyList<HitoResponse>>.NotFound($"El contenedor {containerId} no existe o no pertenece al usuario.");

        var hitos = await unitOfWork.Containers.Query()
            .Where(c => c.Id == containerId)
            .SelectMany(c => c.Hitos)
            .OrderBy(h => h.FechaOcurrencia)
            .ToListAsync(ct);

        return Result<IReadOnlyList<HitoResponse>>.Success(
            hitos.Select(h => h.ToResponse()).ToList());
    }
}
