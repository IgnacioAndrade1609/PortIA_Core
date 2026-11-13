namespace PortIA.Core.Services.Component.Teams.DTOs;

public class TeamDocumentsFilter
{
    public int? AnalystId { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}
