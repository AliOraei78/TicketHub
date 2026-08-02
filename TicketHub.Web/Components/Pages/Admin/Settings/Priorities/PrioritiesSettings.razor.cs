using Fluxor;
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store; // مسیر استیت‌های Fluxor

namespace TicketHub.Web.Components.Pages.Admin.Settings.Priorities;

public partial class PrioritiesSettings
{
    [Inject] public IState<PriorityState> PriState { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;

    protected HashSet<int> selectedIds = new();
    protected bool isBulkDelete = false;
    protected string deleteModalDescription = string.Empty;

    protected PriorityDto priorityModel = new() { ColorCode = "#6B7280", Level = 1 };

    protected bool isEditing = false;
    protected int? editingId = null;

    protected bool showDeleteModal = false;
    protected PriorityDto? itemToDelete;

    protected IEnumerable<PriorityDto> FilteredPriorities =>
        PriState.Value.Priorities
        .Where(p => string.IsNullOrWhiteSpace(PriState.Value.SearchTerm) || p.Name.Contains(PriState.Value.SearchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(p => PriState.Value.SelectedFilterStatus == null || p.IsActive == PriState.Value.SelectedFilterStatus);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Dispatcher.Dispatch(new LoadPrioritiesAction());
    }

    protected void HandleSubmit()
    {
        if (string.IsNullOrWhiteSpace(priorityModel.Name)) return;

        if (!isEditing && string.IsNullOrEmpty(priorityModel.ColorCode))
            priorityModel.ColorCode = "#6B7280";

        Dispatcher.Dispatch(new SavePriorityAction(priorityModel, isEditing));
        CancelEdit();
        ClearMessageAfterDelay();
    }

    protected void EditPriority(PriorityDto item)
    {
        isEditing = true;
        editingId = item.Id;
        priorityModel = item.Adapt<PriorityDto>();
    }

    protected void CancelEdit()
    {
        isEditing = false;
        editingId = null;
        priorityModel = new PriorityDto { ColorCode = "#6B7280", Level = 1 };
    }

    protected void HandleSearch(string term) => Dispatcher.Dispatch(new SetPrioritySearchAction(term));
    protected void FilterByStatus(bool? status) => Dispatcher.Dispatch(new SetPriorityFilterStatusAction(status));

    protected void OnSelectionChanged(HashSet<int> newKeys) => selectedIds = newKeys;
    protected void ClearSelection() => selectedIds.Clear();

    protected void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        deleteModalDescription = $"آیا از حذف {selectedIds.Count} اولویت انتخاب شده مطمئن هستید؟";
        showDeleteModal = true;
    }

    protected void OpenDeleteModal(PriorityDto item)
    {
        itemToDelete = item;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف اولویت «{item.Name}» مطمئن هستید؟";
        showDeleteModal = true;
    }

    protected void CancelDelete()
    {
        showDeleteModal = false;
        itemToDelete = null;
        isBulkDelete = false;
    }

    protected void ConfirmDelete()
    {
        if (isBulkDelete)
        {
            Dispatcher.Dispatch(new DeleteMultiplePrioritiesAction(selectedIds));
            ClearSelection();
        }
        else if (itemToDelete != null)
        {
            Dispatcher.Dispatch(new DeletePriorityAction(itemToDelete.Id));
            selectedIds.Remove(itemToDelete.Id);
            if (isEditing && editingId == itemToDelete.Id) CancelEdit();
        }
        CancelDelete();
        ClearMessageAfterDelay();
    }

    protected void BulkActivatePriorities() => UpdatePrioritiesStatus(true);
    protected void BulkDeactivatePriorities() => UpdatePrioritiesStatus(false);

    private void UpdatePrioritiesStatus(bool isActive)
    {
        Dispatcher.Dispatch(new UpdatePriorityStatusAction(selectedIds, isActive));
        ClearSelection();
        ClearMessageAfterDelay();
    }

    private void ClearMessageAfterDelay()
    {
        _ = Task.Delay(4000).ContinueWith(_ =>
        {
            Dispatcher.Dispatch(new ClearPriorityMessageAction());
        });
    }
}