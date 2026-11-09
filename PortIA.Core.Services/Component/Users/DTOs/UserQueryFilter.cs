namespace PortIA.Core.Services.Component.Users.DTOs;

public class UserQueryFilter
{
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public bool? Active { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}