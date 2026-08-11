using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Statuses;

public partial class StatusForm : ComponentBase
{
    [Parameter, EditorRequired] public StatusDto Model { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Parameter, EditorRequired] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
