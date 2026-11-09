namespace PortIA.Core.Services.Component.Containers.Mappers;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Component.Containers.DTOs;

public static class HitoMapper
{
    public static HitoResponse ToResponse(this Hito entity) =>
        new(entity.Id, entity.Descripcion, entity.Origen, entity.FechaOcurrencia);
}
