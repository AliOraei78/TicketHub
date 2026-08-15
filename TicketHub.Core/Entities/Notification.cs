using System;
using TicketHub.Core.Enums;
using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities;

public class Notification : ISoftDeletable
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.System;
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public int? ReferenceId { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAtUtc { get; set; }

    public virtual User? User { get; set; }
}
