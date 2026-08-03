using Fluxor;
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Permissions; // فضای نام را بر اساس ساختار پروژه تنظیم کنید

public partial class PermissionsSettings
{
    [Inject] public IState<PermissionState> PermState { get; set; } = default!;
    [Inject] public IState<RoleState> RoleState { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;

    protected HashSet<int> selectedIds = new();
    protected PermissionDto permModel = new();
    protected bool isEditing = false, showDeleteModal = false, isBulkDelete = false;
    protected int? editingId = null;
    protected PermissionDto? toDelete;
    protected string deleteModalDesc = string.Empty;

    protected IEnumerable<PermissionDto> FilteredPermissions =>
        PermState.Value.Permissions
            .Where(p => string.IsNullOrWhiteSpace(PermState.Value.SearchTerm) || p.Title.Contains(PermState.Value.SearchTerm))
            .Where(p => PermState.Value.SelectedFilterStatus == null || p.IsActive == PermState.Value.SelectedFilterStatus)
            .Where(p => !PermState.Value.SelectedFilterRoleIds.Any() || (p.RoleIds != null && p.RoleIds.Any(r => PermState.Value.SelectedFilterRoleIds.Contains(r))));

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Dispatcher.Dispatch(new LoadPermissionsAction());
    }

    protected void HandleSubmit()
    {
        if (isEditing && editingId.HasValue) permModel.Id = editingId.Value;
        Dispatcher.Dispatch(new SavePermissionAction(permModel, isEditing));
        CancelEdit();
        ClearMsg();
    }

    protected void Edit(PermissionDto p) { isEditing = true; editingId = p.Id; permModel = p.Adapt<PermissionDto>(); }
    protected void CancelEdit() { isEditing = false; editingId = null; permModel = new(); }

    protected void HandleSearch(string t) => Dispatcher.Dispatch(new SetPermissionSearchAction(t));
    protected void FilterByStatus(bool? s) => Dispatcher.Dispatch(new SetPermissionFilterStatusAction(s));
    protected void FilterByRoles(List<int> r) => Dispatcher.Dispatch(new SetPermissionRoleFilterAction(r));

    protected void OnSelectionChanged(HashSet<int> keys) => selectedIds = keys;
    protected void ClearSelection() => selectedIds.Clear();

    protected void OpenBulkDeleteModal() { isBulkDelete = true; deleteModalDesc = $"حذف {selectedIds.Count} مورد؟"; showDeleteModal = true; }
    protected void OpenDeleteModal(PermissionDto p) { toDelete = p; isBulkDelete = false; deleteModalDesc = $"حذف {p.Title}؟"; showDeleteModal = true; }
    protected void CancelDelete() { showDeleteModal = false; toDelete = null; isBulkDelete = false; }

    protected void ConfirmDelete()
    {
        if (isBulkDelete) { Dispatcher.Dispatch(new DeleteMultiplePermissionsAction(selectedIds.ToList())); ClearSelection(); }
        else if (toDelete != null) { Dispatcher.Dispatch(new DeletePermissionAction(toDelete.Id)); selectedIds.Remove(toDelete.Id); }
        CancelDelete(); ClearMsg();
    }

    protected void BulkUpdateStatus(bool isActive) { Dispatcher.Dispatch(new UpdatePermissionStatusAction(selectedIds.ToList(), isActive)); ClearSelection(); ClearMsg(); }

    private void ClearMsg() => _ = Task.Delay(4000).ContinueWith(_ => Dispatcher.Dispatch(new ClearPermissionMessageAction()));
}