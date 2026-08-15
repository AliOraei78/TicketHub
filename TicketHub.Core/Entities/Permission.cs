using TicketHub.Application.Enums;
using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities;

public class Permission : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ResourceKey { get; set; } // لینک منو یا نام منحصر‌به‌فرد بخش
    public PermissionType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}