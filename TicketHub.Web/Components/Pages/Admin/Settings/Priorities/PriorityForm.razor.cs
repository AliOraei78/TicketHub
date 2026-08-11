using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Priorities;

public partial class PriorityForm : ComponentBase
{
    [Parameter, EditorRequired] public PriorityDto Model { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Parameter, EditorRequired] public EventCallback OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
