// TicketHub.Core/Entities/WorkflowStatus.cs (فایل جدید ایجاد کنید)
namespace TicketHub.Core.Entities;

public class WorkflowStatus
{
    public int Id { get; set; } // باید کلید اصلی شود (تغییر در DbContext نیاز است)
    public Guid NodeId { get; set; } // شناسه یکتای این گره در بوم
    public bool IsInitial { get; set; } = false;
    public bool IsFinal { get; set; } = false;
    public int WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = null!;

    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;

    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    
    public ICollection<Transition> FromTransitions { get; set; } = new List<Transition>();
    public ICollection<Transition> ToTransitions { get; set; } = new List<Transition>();
}