using TicketHub.Core.Entities;

namespace TicketHub.Core.Entities;

public class WorkflowTransition
{
    public int WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = null!;

    public int TransitionId { get; set; }
    public Transition Transition { get; set; } = null!;
}