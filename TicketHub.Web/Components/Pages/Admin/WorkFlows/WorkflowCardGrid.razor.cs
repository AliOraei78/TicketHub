using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows;

public partial class WorkflowCardGrid : ComponentBase
{
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IPermissionService PermissionService { get; set; } = default!;

    [Parameter] public List<WorkflowDto> Workflows { get; set; } = new();
    [Parameter] public bool IsLoading { get; set; }

    [Parameter] public EventCallback<WorkflowDto> OnEdit { get; set; }
    [Parameter] public EventCallback<WorkflowDto> OnDelete { get; set; }

    [Parameter] public HashSet<int> SelectedIds { get; set; } = new();
    [Parameter] public EventCallback<HashSet<int>> SelectedIdsChanged { get; set; }

    [Parameter] public HashSet<int> DeletingIds { get; set; } = new();

    protected bool CanManage { get; set; } = false;

    protected override async Task OnParametersSetAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        CanManage = await PermissionService.HasAccessAsync(authState.User, "/workflows", PermissionType.SystemSection);
    }

    protected async Task ToggleSelection(int id, object? value)
    {
        bool isChecked = (bool)(value ?? false);
        if (isChecked) SelectedIds.Add(id);
        else SelectedIds.Remove(id);

        await SelectedIdsChanged.InvokeAsync(SelectedIds);
    }
}
