using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Facades;
using TicketHub.Web.State;

namespace TicketHub.Web.Components.Pages.Admin.Settings;

public partial class RolesSettings : ComponentBase, IDisposable
{
    [Inject] protected RoleFacade Facade { get; set; } = default!;
    [Inject] protected RoleState State { get; set; } = default!;

    protected IEnumerable<RoleDto> FilteredRoles =>
        (State.Roles ?? Enumerable.Empty<RoleDto>())
        .Where(r => string.IsNullOrWhiteSpace(State.SearchTerm) || r.Name.Contains(State.SearchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(r => State.SelectedFilterStatus == null || r.IsActive == State.SelectedFilterStatus);

    protected override async Task OnInitializedAsync()
    {
        State.OnChange += StateHasChanged;
        await Facade.LoadRolesAsync();
    }

    public void Dispose() => State.OnChange -= StateHasChanged;

    protected async Task HandleSubmitRole()
    {
        await Facade.SubmitRoleAsync();
        ClearMessageAfterDelay(3000);
    }

    protected async Task ConfirmDelete()
    {
        await Facade.ConfirmDeleteAsync();
        ClearMessageAfterDelay(4000);
    }

    protected async Task BulkDeactivateRoles()
    {
        await Facade.BulkDeactivateAsync();
        ClearMessageAfterDelay(4000);
    }

    protected async Task BulkActivateRoles()
    {
        await Facade.BulkActivateAsync();
        ClearMessageAfterDelay(4000);
    }

    private void ClearMessageAfterDelay(int delay)
    {
        _ = Task.Delay(delay).ContinueWith(_ => { State.SuccessMessage = null; State.IsError = false; InvokeAsync(StateHasChanged); });
    }

    protected void FilterByStatus(bool? status) { State.SelectedFilterStatus = status; State.NotifyStateChanged(); }
    protected void HandleSearch(string term) { State.SearchTerm = term; State.NotifyStateChanged(); }
    protected void OnSelectionChanged(HashSet<int> newKeys) { State.SelectedRoleIds = newKeys; State.NotifyStateChanged(); }
    protected void ClearSelection() { State.SelectedRoleIds.Clear(); State.NotifyStateChanged(); }
    protected void EditRole(RoleDto role) { State.IsEditing = true; State.EditingRoleId = role.Id; State.RoleModel = role.Adapt<RoleDto>(); State.NotifyStateChanged(); }
    protected void CancelEdit() => State.ClearForm();
    protected void OpenBulkDeleteModal() { State.IsBulkDelete = true; State.DeleteModalDescription = $"آیا از حذف {State.SelectedRoleIds.Count} نقش انتخاب شده مطمئن هستید؟ این عملیات غیرقابل بازگشت است."; State.ShowDeleteModal = true; State.NotifyStateChanged(); }
    protected void OpenDeleteModal(RoleDto role) { State.RoleToDelete = role; State.IsBulkDelete = false; State.DeleteModalDescription = $"آیا از حذف نقش «{role.Name}» مطمئن هستید؟ این عملیات غیرقابل بازگشت است."; State.ShowDeleteModal = true; State.NotifyStateChanged(); }
    protected void CancelDelete() => State.ClearDeleteModal();
}