// TicketHub.Core/Entities/WorkflowStatus.cs (فایل جدید ایجاد کنید)
namespace TicketHub.Core.Entities;

public class WorkflowStatus
{
    public int WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = null!;

    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;

    public double PositionX { get; set; }
    public double PositionY { get; set; }
}