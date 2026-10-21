namespace PortIA.Core.Services.Component.Containers.DTOs;

public record RiskSummaryDto(
    int OK,
    int WARNING,
    int CRITICAL,
    int OVERDUE
);

public record NextDeadlineDto(
    int ContainerId,
    string ContainerNumber,
    string DeadlineType, // "DEMURRAGE" o "DETENTION"
    DateTime Deadline
);

public record ContainerSummaryMetricsResponse(
    int Total,
    RiskSummaryDto ByRisk,
    int NeedsReview,
    int Unmanaged,
    List<NextDeadlineDto> NextDeadlines
);