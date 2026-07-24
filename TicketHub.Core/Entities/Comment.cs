namespace TicketHub.Core.Entities;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Comment.cs
    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? TicketHistoryId { get; set; }
    public TicketHistory? TicketHistory { get; set; }
}