namespace PortIA.Core.Services.Component.Navieras.Mappers;

using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Services.Component.Navieras.DTOs;

public static class NavieraMapper
{
    public static Naviera ToEntity(this CreateNavieraRequest request) => new()
    {
        NombreNaviera = request.NombreNaviera.Trim(),
        CodigoNaviera = Normalizar(request.CodigoNaviera),
        Email = Normalizar(request.Email),
        Telefono = Normalizar(request.Telefono),
        Vigente = true
    };

    public static void Apply(this Naviera naviera, UpdateNavieraRequest request)
    {
        naviera.NombreNaviera = request.NombreNaviera.Trim();
        naviera.CodigoNaviera = Normalizar(request.CodigoNaviera);
        naviera.Email = Normalizar(request.Email);
        naviera.Telefono = Normalizar(request.Telefono);
    }

    public static NavieraDto ToDto(this Naviera naviera) => new()
    {
        Id = naviera.Id,
        NombreNaviera = naviera.NombreNaviera,
        CodigoNaviera = naviera.CodigoNaviera,
        Email = naviera.Email,
        Telefono = naviera.Telefono,
        Vigente = naviera.Vigente
    };

    public static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
