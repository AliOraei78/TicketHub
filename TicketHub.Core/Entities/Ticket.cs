using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities
{
    public class Ticket : ISoftDeletable
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAtUtc { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Add these lines for the new relation:
        public int StatusId { get; set; }
        public Status Status { get; set; } = null!;

        public int? CategoryId { get; set; }
        public Category? Category { get; set; } = null!;

        public int? PriorityId { get; set; }
        public Priority? Priority { get; set; } = null!;

        public int? WorkflowStatusId { get; set; }
        public WorkflowStatus? WorkflowStatus { get; set; }

        public DateTime? DueDate { get; set; }

        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<TicketFieldValue> FieldValues { get; set; } = new List<TicketFieldValue>();
    }
}