using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Roles;

public partial class RoleForm : ComponentBase
{
    [Parameter] public RoleDto Model { get; set; } = new();
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public string? SuccessMessage { get; set; }
    [Parameter] public bool IsError { get; set; }
    [Parameter] public EventCallback OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
