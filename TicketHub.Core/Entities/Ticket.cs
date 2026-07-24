// TicketHub.Core/Entities/Ticket.cs
namespace TicketHub.Core.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public int? ProjectId { get; set; }
        // Add these lines for the new relation:
        public int StatusId { get; set; }
        public Status Status { get; set; } = null!;
        public int? CategoryId { get; set; }

        public int PriorityId { get; set; }
        public Priority Priority { get; set; } = null!;
        public Category? Category { get; set; }
        public User User { get; set; } = null!;
        public Project? Project { get; set; } = null!;
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}