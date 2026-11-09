namespace PortIA.Core.Services.Component.Users.DTOs;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Active { get; set; }
}