namespace PortIA.Core.Services.Component.Containers.DTOs;

public record HitoResponse(
    int Id,
    string Descripcion,
    string Origen,
    DateTimeOffset FechaOcurrencia
);
