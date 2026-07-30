namespace TicketHub.Application.DTOs;

public class WorkflowDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public List<TransitionDto> Transitions { get; set; } = new();
    public List<WorkflowStatusDto> WorkflowStatuses { get; set; } = new();
    public List<ProjectDto> Projects { get; set; } = new();
}