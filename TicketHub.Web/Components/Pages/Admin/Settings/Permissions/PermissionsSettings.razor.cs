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

    // Telemetry Statistics
    protected int TotalPermissionsCount => PermState.Value.Permissions?.Count() ?? 0;
    protected int ActivePermissionsCount => PermState.Value.Permissions?.Count(p => p.IsActive) ?? 0;
    protected int InactivePermissionsCount => PermState.Value.Permissions?.Count(p => !p.IsActive) ?? 0;

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
    }

    protected void Edit(PermissionDto p) { isEditing = true; editingId = p.Id; permModel = p.Adapt<PermissionDto>(); }
    protected void CancelEdit() { isEditing = false; editingId = null; permModel = new(); }

    protected void HandleSearch(string t) => Dispatcher.Dispatch(new SetPermissionSearchAction(t));
    protected void FilterByStatus(bool? s) => Dispatcher.Dispatch(new SetPermissionFilterStatusAction(s));
    protected void FilterByRoles(List<int> r) => Dispatcher.Dispatch(new SetPermissionRoleFilterAction(r));

    protected void OnSelectionChanged(HashSet<int> keys) => selectedIds = new HashSet<int>(keys);
    protected void ClearSelection() => selectedIds = new HashSet<int>();

    protected void OpenBulkDeleteModal() { isBulkDelete = true; deleteModalDesc = $"آیا از حذف {selectedIds.Count} دسترسی انتخاب شده مطمئن هستید؟"; showDeleteModal = true; }
    protected void OpenDeleteModal(PermissionDto p) { toDelete = p; isBulkDelete = false; deleteModalDesc = $"آیا از حذف دسترسی «{p.Title}» مطمئن هستید؟"; showDeleteModal = true; }
    protected void CancelDelete() { showDeleteModal = false; toDelete = null; isBulkDelete = false; }

    protected void ConfirmDelete()
    {
        if (isBulkDelete) { Dispatcher.Dispatch(new DeleteMultiplePermissionsAction(selectedIds.ToList())); ClearSelection(); }
        else if (toDelete != null)
        {
            Dispatcher.Dispatch(new DeletePermissionAction(toDelete.Id));
            var newSelection = new HashSet<int>(selectedIds);
            newSelection.Remove(toDelete.Id);
            selectedIds = newSelection;
        }
        CancelDelete();
    }

    protected void BulkUpdateStatus(bool isActive)
    {
        Dispatcher.Dispatch(new UpdatePermissionStatusAction(selectedIds.ToList(), isActive));
        ClearSelection();
    }

    protected string GetPermissionTypeAvatarClass(TicketHub.Application.Enums.PermissionType type) => type switch
    {
        TicketHub.Application.Enums.PermissionType.Menu => "bg-cyan-500/10 border border-cyan-400/25 text-cyan-300 shadow-[0_0_8px_rgba(56,189,248,0.2)]",
        TicketHub.Application.Enums.PermissionType.SystemSection => "bg-indigo-500/10 border border-indigo-400/25 text-indigo-300 shadow-[0_0_8px_rgba(99,102,241,0.2)]",
        TicketHub.Application.Enums.PermissionType.Full => "bg-amber-500/10 border border-amber-400/25 text-amber-300 shadow-[0_0_8px_rgba(245,158,11,0.2)]",
        _ => "bg-cyan-500/10 border border-cyan-400/25 text-cyan-300 shadow-[0_0_8px_rgba(56,189,248,0.2)]"
    };

    protected string GetPermissionTypeBadgeClass(TicketHub.Application.Enums.PermissionType type) => type switch
    {
        TicketHub.Application.Enums.PermissionType.Menu => "bg-cyan-500/15 border-cyan-400/30 text-cyan-300",
        TicketHub.Application.Enums.PermissionType.SystemSection => "bg-indigo-500/15 border-indigo-400/30 text-indigo-300",
        TicketHub.Application.Enums.PermissionType.Full => "bg-amber-500/15 border-amber-400/30 text-amber-300",
        _ => "bg-cyan-500/15 border-cyan-400/30 text-cyan-300"
    };

    protected string GetPermissionTypeName(TicketHub.Application.Enums.PermissionType type) => type switch
    {
        TicketHub.Application.Enums.PermissionType.Menu => "منو (مشاهده)",
        TicketHub.Application.Enums.PermissionType.SystemSection => "بخش سیستم",
        TicketHub.Application.Enums.PermissionType.Full => "دسترسی کامل",
        _ => type.ToString()
    };

    protected RenderFragment GetPermissionTypeIcon(TicketHub.Application.Enums.PermissionType type) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "xmlns", "http://www.w3.org/2000/svg");
        builder.AddAttribute(2, "class", "h-4 w-4");
        builder.AddAttribute(3, "fill", "none");
        builder.AddAttribute(4, "viewBox", "0 0 24 24");
        builder.AddAttribute(5, "stroke", "currentColor");

        builder.OpenElement(6, "path");
        builder.AddAttribute(7, "stroke-linecap", "round");
        builder.AddAttribute(8, "stroke-linejoin", "round");
        builder.AddAttribute(9, "stroke-width", "2");

        var pathD = type switch
        {
            TicketHub.Application.Enums.PermissionType.Menu => "M4 6h16M4 12h16M4 18h7",
            TicketHub.Application.Enums.PermissionType.SystemSection => "M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4",
            TicketHub.Application.Enums.PermissionType.Full => "M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z",
            _ => "M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"
        };

        builder.AddAttribute(10, "d", pathD);
        builder.CloseElement();
        builder.CloseElement();
    };
}