namespace PortIA.Core.Services.Component.Containers.Mappers;

using PortIA.Core.Services.Component.Containers.DTOs;
using PortIA.Core.Services.Component.Riesgo.Configuration;

public static class ContainerMapper
{
    private const string NivelOk = "OK";
    private const string NivelWarning = "WARNING";
    private const string NivelCritical = "CRITICAL";
    private const string NivelOverdue = "OVERDUE";
    private const string EstadoDevueltoVacio = "DEVUELTO_VACIO";

    public static ContainerSummaryResponse ToSummaryResponse(this ContainerSummaryProjection projection, RiskEngineOptions umbrales)
    {
        var demurrageDeadlineUtc = projection.DeadlineDemurrage?.UtcDateTime;
        long? hoursToDeadline = demurrageDeadlineUtc.HasValue
            ? (long)Math.Ceiling((demurrageDeadlineUtc.Value - DateTime.UtcNow).TotalHours)
            : null;

        return new(
            Id: projection.Id,
            ContainerNumber: projection.NumeroContainer,
            BlNumber: projection.NumeroBl,
            ForwarderName: projection.NombreForwarder,
            ShippingLine: projection.IdNaviera.HasValue 
                ? new ShippingLineDto(projection.IdNaviera.Value, projection.NavieraNombre ?? "N/A") 
                : null,
            Eta: projection.FechaLlegadaEstimada?.UtcDateTime,
            ArrivalDate: projection.FechaArriboReal?.UtcDateTime,
            PickupDate: projection.FechaRetiroContainer?.UtcDateTime,
            DemurrageFreeDays: projection.DiasLibresDemurrage,
            DetentionFreeDays: projection.DiasLibresDetention,
            DemurrageDeadline: demurrageDeadlineUtc,
            DetentionDeadline: projection.DeadlineDetention?.UtcDateTime,
            RiskLevel: CalcularNivelRiesgoEnVivo(projection, umbrales),
            HoursToDeadline: hoursToDeadline,
            NeedsReview: projection.NeedsReview,
            Managed: projection.Managed,
            PortStatus: projection.EstadoPuerto,
            Estado: projection.Estado,
            AssignedTo: projection.AsignadoA.HasValue 
                ? new AssignedToDto(projection.AsignadoA.Value, projection.AsignadoANombre ?? "N/A") 
                : null,
            OriginTerminal: projection.IdTerminalOrigen.HasValue
                ? new TerminalDto(projection.IdTerminalOrigen.Value, projection.TerminalOrigenNombre ?? "N/A")
                : null,
            DestinationTerminal: projection.IdTerminalLlegada.HasValue
                ? new TerminalDto(projection.IdTerminalLlegada.Value, projection.TerminalLlegadaNombre ?? "N/A")
                : null
        );
    }

    private static string CalcularNivelRiesgoEnVivo(ContainerSummaryProjection projection, RiskEngineOptions umbrales)
    {
        if (projection.Estado == EstadoDevueltoVacio)
        {
            return NivelOk;
        }

        DateTimeOffset? deadlineActivo = projection.FechaRetiroContainer is not null
            ? projection.DeadlineDetention
            : projection.FechaArriboReal is not null
                ? projection.DeadlineDemurrage
                : null;

        if (deadlineActivo is null)
        {
            return NivelOk;
        }

        var horasRestantes = (deadlineActivo.Value - DateTimeOffset.UtcNow).TotalHours;

        if (horasRestantes > umbrales.HorasUmbralOk) return NivelOk;
        if (horasRestantes > umbrales.HorasUmbralWarning) return NivelWarning;
        if (horasRestantes >= umbrales.HorasUmbralCritical) return NivelCritical;
        return NivelOverdue;
    }
}