using Fluxor;
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Roles;

public partial class RolesSettings
{
    [Inject] public IState<RoleState> RolState { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;

    // UI Variables
    protected RoleDto roleModel = new();
    protected bool isEditing = false;
    protected int? editingRoleId = null;

    protected HashSet<int> selectedRoleIds = new();
    protected bool isBulkDelete = false;
    protected string deleteModalDescription = string.Empty;
    protected bool showDeleteModal = false;
    protected RoleDto? roleToDelete;

    protected IEnumerable<RoleDto> FilteredRoles =>
        RolState.Value.Roles
        .Where(r => string.IsNullOrWhiteSpace(RolState.Value.SearchTerm) || r.Name.Contains(RolState.Value.SearchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(r => RolState.Value.SelectedFilterStatus == null || r.IsActive == RolState.Value.SelectedFilterStatus);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Dispatcher.Dispatch(new LoadRolesAction());
    }

    protected void HandleSubmitRole()
    {
        if (string.IsNullOrWhiteSpace(roleModel.Name)) return;

        Dispatcher.Dispatch(new SaveRoleAction(roleModel, isEditing, editingRoleId));
        CancelEdit();
        ClearMessageAfterDelay();
    }

    protected void EditRole(RoleDto role)
    {
        isEditing = true;
        editingRoleId = role.Id;
        roleModel = role.Adapt<RoleDto>();
    }

    protected void CancelEdit()
    {
        isEditing = false;
        editingRoleId = null;
        roleModel = new RoleDto();
    }

    protected void HandleSearch(string term) => Dispatcher.Dispatch(new SetRoleSearchAction(term));
    protected void FilterByStatus(bool? status) => Dispatcher.Dispatch(new SetRoleFilterStatusAction(status));

    protected void OnSelectionChanged(HashSet<int> newKeys) => selectedRoleIds = newKeys;
    protected void ClearSelection() => selectedRoleIds.Clear();

    protected void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        deleteModalDescription = $"آیا از حذف {selectedRoleIds.Count} نقش انتخاب شده مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        showDeleteModal = true;
    }

    protected void OpenDeleteModal(RoleDto role)
    {
        roleToDelete = role;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف نقش «{role.Name}» مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        showDeleteModal = true;
    }

    protected void CancelDelete()
    {
        showDeleteModal = false;
        roleToDelete = null;
        isBulkDelete = false;
    }

    protected void ConfirmDelete()
    {
        if (isBulkDelete)
        {
            Dispatcher.Dispatch(new DeleteMultipleRolesAction(selectedRoleIds));
            ClearSelection();
        }
        else if (roleToDelete != null)
        {
            Dispatcher.Dispatch(new DeleteRoleAction(roleToDelete.Id));
            selectedRoleIds.Remove(roleToDelete.Id);
            if (isEditing && editingRoleId == roleToDelete.Id) CancelEdit();
        }

        CancelDelete();
        ClearMessageAfterDelay();
    }

    protected void BulkDeactivateRoles() => UpdateRolesStatus(false);
    protected void BulkActivateRoles() => UpdateRolesStatus(true);

    private void UpdateRolesStatus(bool isActive)
    {
        Dispatcher.Dispatch(new UpdateRoleStatusAction(selectedRoleIds, isActive));
        ClearSelection();
        ClearMessageAfterDelay();
    }

    private void ClearMessageAfterDelay()
    {
        _ = Task.Delay(4000).ContinueWith(_ =>
        {
            Dispatcher.Dispatch(new ClearRoleMessageAction());
        });
    }
}