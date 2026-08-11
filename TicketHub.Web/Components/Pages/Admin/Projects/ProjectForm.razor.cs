using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.Projects;

public partial class ProjectForm : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public ProjectDto Model { get; set; } = new();
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public IEnumerable<WorkflowDto> Workflows { get; set; } = Array.Empty<WorkflowDto>();
    [Parameter] public IEnumerable<RoleDto> Roles { get; set; } = Array.Empty<RoleDto>();
    [Parameter] public EventCallback OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
