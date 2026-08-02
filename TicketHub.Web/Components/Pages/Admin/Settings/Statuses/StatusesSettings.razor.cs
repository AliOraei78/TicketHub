using Fluxor;
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Statuses;

public partial class StatusesSettings
{
    [Inject] public IState<StatusState> StatState { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;

    // UI Variables
    protected StatusDto statusModel = new() { ColorCode = "#3b82f6" };
    protected bool isEditing = false;
    protected int? editingStatusId = null;

    protected HashSet<int> selectedStatusIds = new();
    protected bool isBulkDelete = false;
    protected string deleteModalDescription = string.Empty;
    protected bool showDeleteModal = false;
    protected StatusDto? statusToDelete;

    protected IEnumerable<StatusDto> FilteredStatuses =>
        StatState.Value.Statuses
        .Where(s => string.IsNullOrWhiteSpace(StatState.Value.SearchTerm) || s.Name.Contains(StatState.Value.SearchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(s => StatState.Value.SelectedFilterStatus == null || s.IsActive == StatState.Value.SelectedFilterStatus);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Dispatcher.Dispatch(new LoadStatusesAction());
    }

    protected void HandleSubmitStatus()
    {
        if (string.IsNullOrWhiteSpace(statusModel.Name)) return;

        if (isEditing && editingStatusId.HasValue)
            statusModel.Id = editingStatusId.Value;
        else
            statusModel.ColorCode = string.IsNullOrEmpty(statusModel.ColorCode) ? "#3b82f6" : statusModel.ColorCode;

        Dispatcher.Dispatch(new SaveStatusAction(statusModel, isEditing));
        CancelEdit();
        ClearMessageAfterDelay();
    }

    protected void EditStatus(StatusDto status)
    {
        isEditing = true;
        editingStatusId = status.Id;
        statusModel = status.Adapt<StatusDto>();
    }

    protected void CancelEdit()
    {
        isEditing = false;
        editingStatusId = null;
        statusModel = new StatusDto { ColorCode = "#3b82f6", NeedApproval = false };
    }

    protected void HandleSearch(string term) => Dispatcher.Dispatch(new SetStatusSearchAction(term));
    protected void FilterByStatus(bool? status) => Dispatcher.Dispatch(new SetStatusFilterStatusAction(status));

    protected void OnSelectionChanged(HashSet<int> newKeys) => selectedStatusIds = newKeys;
    protected void ClearSelection() => selectedStatusIds.Clear();

    protected void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        deleteModalDescription = $"آیا از حذف {selectedStatusIds.Count} وضعیت انتخاب شده مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        showDeleteModal = true;
    }

    protected void OpenDeleteModal(StatusDto status)
    {
        statusToDelete = status;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف وضعیت «{status.Name}» مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        showDeleteModal = true;
    }

    protected void CancelDelete()
    {
        showDeleteModal = false;
        statusToDelete = null;
        isBulkDelete = false;
    }

    protected void ConfirmDelete()
    {
        if (isBulkDelete)
        {
            Dispatcher.Dispatch(new DeleteMultipleStatusesAction(selectedStatusIds));
            ClearSelection();
        }
        else if (statusToDelete != null)
        {
            Dispatcher.Dispatch(new DeleteStatusAction(statusToDelete.Id));
            selectedStatusIds.Remove(statusToDelete.Id);
            if (isEditing && editingStatusId == statusToDelete.Id) CancelEdit();
        }

        CancelDelete();
        ClearMessageAfterDelay();
    }

    protected void BulkActivateStatuses() => UpdateStatusesStatus(true);
    protected void BulkDeactivateStatuses() => UpdateStatusesStatus(false);

    private void UpdateStatusesStatus(bool isActive)
    {
        Dispatcher.Dispatch(new UpdateStatusesStatusAction(selectedStatusIds, isActive));
        ClearSelection();
        ClearMessageAfterDelay();
    }

    private void ClearMessageAfterDelay()
    {
        _ = Task.Delay(4000).ContinueWith(_ =>
        {
            Dispatcher.Dispatch(new ClearStatusMessageAction());
        });
    }
}