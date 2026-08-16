using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor;

public partial class StatusSidebar : ComponentBase
{
    [Parameter] public IEnumerable<StatusDto> Statuses { get; set; } = new List<StatusDto>();
    [Parameter] public EventCallback<StatusDto> OnDragStart { get; set; }
    [Parameter] public EventCallback<StatusDto> OnStatusAdd { get; set; }

    protected string SearchTerm { get; set; } = string.Empty;
    protected IEnumerable<StatusDto> FilteredStatuses => string.IsNullOrEmpty(SearchTerm)
        ? Statuses
        : Statuses.Where(s => s.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
}
