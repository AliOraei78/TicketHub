using TicketHub.Core.Entities;

namespace TicketHub.Application.DTOs;

public class TransitionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourcePort { get; set; } = "Right";
    public string TargetPort { get; set; } = "Left";

    public int FromState { get; set; }
    public int ToState { get; set; }
    public int WorkflowId { get; set; }

    public int IsAutomated { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ActivateAt { get; set; }
    public int? DeadlineMinutes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }

    public List<int> AllowedRoleIds { get; set; } = new();
    public List<TransitionFieldDto> TransitionFields { get; set; } = new();

}