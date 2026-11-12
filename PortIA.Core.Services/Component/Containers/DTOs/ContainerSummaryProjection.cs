namespace PortIA.Core.Services.Component.Containers.DTOs;

public record ContainerSummaryProjection(
    int Id,
    string NumeroContainer,
    string? NumeroBl,
    string? NombreForwarder,
    int? IdNaviera,
    string? NavieraNombre,
    int? IdTerminalOrigen,
    string? TerminalOrigenNombre,
    int? IdTerminalLlegada,
    string? TerminalLlegadaNombre,
    DateTimeOffset? FechaLlegadaEstimada,
    DateTimeOffset? FechaArriboReal,
    DateTimeOffset? FechaRetiroContainer,
    int? DiasLibresDemurrage,
    int? DiasLibresDetention,
    DateTimeOffset? DeadlineDemurrage,
    DateTimeOffset? DeadlineDetention,
    string? RiskLevel,
    bool NeedsReview,
    bool Managed,
    string? EstadoPuerto,
    string? Estado,
    int? AsignadoA,
    string? AsignadoANombre,
    DateTimeOffset CreateAt
);