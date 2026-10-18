namespace PortIA.Core.Services.Component.Containers.DTOs;

public record ShippingLineDto(int Id, string Name);
public record AssignedToDto(int Id, string Name);

public record ContainerSummaryResponse(
    int Id,
    string ContainerNumber,
    string? BlNumber,
    ShippingLineDto? ShippingLine,
    DateTime? Eta,
    DateTime? ArrivalDate,
    DateTime? PickupDate,
    int? DemurrageFreeDays,
    int? DetentionFreeDays,
    DateTime? DemurrageDeadline,
    DateTime? DetentionDeadline,
    string? RiskLevel,
    long? HoursToDeadline,
    bool NeedsReview,
    bool Managed,
    string? PortStatus,
    AssignedToDto? AssignedTo
);