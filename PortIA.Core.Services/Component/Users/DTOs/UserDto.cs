namespace PortIA.Core.Services.Component.Users.DTOs;

public class RoleSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public RoleSummaryDto Role { get; set; } = null!;
    public bool Active { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}