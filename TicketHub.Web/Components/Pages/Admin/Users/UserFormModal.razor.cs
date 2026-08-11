using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TicketHub.Application.DTOs;
using TicketHub.Application.Models;

namespace TicketHub.Web.Components.Pages.Admin.Users;

public partial class UserFormModal : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public UserDto Model { get; set; } = new();
    [Parameter] public string PasswordInput { get; set; } = string.Empty;
    [Parameter] public List<RoleDto> AvailableRoles { get; set; } = new();
    [Parameter] public List<string> SelectedRoles { get; set; } = new();
    [Parameter] public EventCallback<UserFormSubmissionResult> OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    protected UserFormSubmissionResult FormModel { get; set; } = new();
    protected EditContext EditContext { get; set; } = default!;

    protected override void OnInitialized()
    {
        EditContext = new EditContext(FormModel);
    }

    protected override void OnParametersSet()
    {
        if (IsOpen && FormModel.User != Model)
        {
            FormModel.User = Model;
            FormModel.Password = PasswordInput;
            FormModel.SelectedRoles = SelectedRoles;
            EditContext = new EditContext(FormModel);
        }
    }

    protected async Task HandleSubmit()
    {
        if (EditContext.Validate())
        {
            await OnSubmit.InvokeAsync(FormModel);
        }
    }
}
