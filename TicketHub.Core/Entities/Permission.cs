using TicketHub.Application.Enums;

namespace TicketHub.Core.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ResourceKey { get; set; } // لینک منو یا نام منحصر‌به‌فرد بخش
    public PermissionType Type { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}