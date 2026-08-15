using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities;

public class Comment : ISoftDeletable
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAtUtc { get; set; }

    // Comment.cs
    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}