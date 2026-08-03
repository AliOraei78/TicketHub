using TicketHub.Application.Enums;

namespace TicketHub.Application.DTOs;

public class PermissionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ResourceKey { get; set; }
    public PermissionType Type { get; set; }
    public bool IsActive { get; set; }

    public List<int> RoleIds { get; set; } = new List<int>(); 
}
