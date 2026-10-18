namespace PortIA.Core.Services.Component.Containers.Mappers;

using PortIA.Core.Services.Component.Containers.DTOs;

public static class ContainerMapper
{
    public static ContainerSummaryResponse ToSummaryResponse(this ContainerSummaryProjection projection)
    {
        var demurrageDeadlineUtc = projection.DeadlineDemurrage?.UtcDateTime;
        long? hoursToDeadline = demurrageDeadlineUtc.HasValue
            ? (long)Math.Ceiling((demurrageDeadlineUtc.Value - DateTime.UtcNow).TotalHours)
            : null;

        return new(
            Id: projection.Id,
            ContainerNumber: projection.NumeroContainer,
            BlNumber: projection.NumeroBl,
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
            RiskLevel: projection.RiskLevel,
            HoursToDeadline: hoursToDeadline,
            NeedsReview: projection.NeedsReview,
            Managed: projection.Managed,
            PortStatus: projection.EstadoPuerto,
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
}