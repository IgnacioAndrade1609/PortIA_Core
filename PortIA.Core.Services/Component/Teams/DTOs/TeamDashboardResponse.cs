namespace PortIA.Core.Services.Component.Teams.DTOs;

public record ContainersByRiskDto(string RiskLevel, int Count);

public record AlertsByAnalystDto(int AnalystId, string AnalystName, int Generated, int Managed);

public record DocumentsByDayDto(DateOnly Date, int Count);

public record TeamDashboardResponse(
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    List<ContainersByRiskDto> ContainersByRisk,
    List<AlertsByAnalystDto> AlertsByAnalyst,
    double? AvgReactionTimeCriticalMinutes,
    List<DocumentsByDayDto> DocumentsByPeriod
);
