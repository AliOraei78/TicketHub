using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Components.Pages.Admin.Users;

public partial class UserTable : ComponentBase
{
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IPermissionService PermissionService { get; set; } = default!;

    [Parameter] public List<UserDto> Users { get; set; } = new();
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback<UserDto> OnEdit { get; set; }
    [Parameter] public EventCallback<UserDto> OnDelete { get; set; }

    [Parameter] public int CurrentPage { get; set; } = 1;
    [Parameter] public int PageSize { get; set; } = 10;

    [Parameter] public HashSet<int> SelectedIds { get; set; } = new();
    [Parameter] public EventCallback<HashSet<int>> SelectedIdsChanged { get; set; }
    [Parameter] public HashSet<int> DeletingIds { get; set; } = new();
    [Parameter] public string ResourceKey { get; set; } = "/users";

    protected bool CanManage { get; set; } = false;

    protected override async Task OnParametersSetAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        CanManage = await PermissionService.HasAccessAsync(authState.User, ResourceKey, PermissionType.SystemSection);
    }

    protected async Task OnSelectionChanged(HashSet<int> newKeys)
    {
        SelectedIds = newKeys;
        await SelectedIdsChanged.InvokeAsync(SelectedIds);
    }
}
