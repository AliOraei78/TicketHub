using TicketHub.Core.Interfaces;

namespace TicketHub.Core.Entities;

public class Workflow : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Transition> Transitions { get; set; } = new List<Transition>();
    public ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>(); // این خط اضافه شود
    public ICollection<WorkflowStatus> WorkflowStatuses { get; set; } = new List<WorkflowStatus>();
}