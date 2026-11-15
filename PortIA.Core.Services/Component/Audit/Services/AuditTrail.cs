namespace PortIA.Core.Services.Component.Audit.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Component.Audit.Interfaces;

public class AuditTrail(IUnitOfWork unitOfWork, ILogger<AuditTrail> logger) : IAuditTrail
{
    private const string EntidadSesion = "Sesion";
    private const string AccionCrear = "CREAR";

    public async Task RecordAsync(string entidad, int idEntidad, string accion, int? responsable, object? anteriores, object? nuevos, CancellationToken ct)
    {
        try
        {
            var accionEntidad = await unitOfWork.Acciones.GetByDescripcionAsync(accion, ct);
            if (accionEntidad is null)
            {
                logger.LogWarning("No se encontró la acción {Accion}, no se registra auditoría de {Entidad} {Id}", accion, entidad, idEntidad);
                return;
            }

            await unitOfWork.LogsAuditoria.AddAsync(new LogAuditoria
            {
                NombreEntidad = entidad,
                IdEntidad = idEntidad,
                IdAccion = accionEntidad.Id,
                Responsable = responsable,
                ValoresAnteriores = anteriores is null ? null : JsonSerializer.Serialize(anteriores),
                ValoresNuevos = nuevos is null ? null : JsonSerializer.Serialize(nuevos)
            }, ct);

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo registrar la auditoría de {Entidad} {Id}; la operación principal no se ve afectada.", entidad, idEntidad);
        }
    }

    public Task RecordLoginAsync(int usuarioId, CancellationToken ct) =>
        RecordAsync(EntidadSesion, usuarioId, AccionCrear, usuarioId, null, new { Evento = "INICIO_SESION" }, ct);

    public async Task RecordFailedLoginAsync(string email, CancellationToken ct)
    {
        try
        {
            var usuario = await unitOfWork.Usuarios.GetByEmailAsync(email, ct);
            if (usuario is null) return;

            await RecordAsync(EntidadSesion, usuario.Id, AccionCrear, null, null,
                new { Evento = "INICIO_SESION_FALLIDO", Correo = email }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo registrar el inicio de sesión fallido.");
        }
    }
}

public static class AuditSnapshots
{
    public static object Usuario(string nombre, string correo, string rol, bool activo, string? jefe) =>
        new { Nombre = nombre, Correo = correo, Rol = rol, Activo = activo, Jefe = jefe };

    public static object Naviera(string nombre, string? codigo, string? correo, string? telefono, bool vigente) =>
        new { Nombre = nombre, Codigo = codigo, Correo = correo, Telefono = telefono, Vigente = vigente };

    public static object Terminal(string codigo, string nombre, string? pais, bool vigente) =>
        new { Codigo = codigo, Nombre = nombre, Pais = pais, Vigente = vigente };
}
