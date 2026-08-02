using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Facades;
using Mapster;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Statuses;

public partial class StatusesSettings : ComponentBase, IDisposable
{
    [Inject]
    private StatusFacade Facade { get; set; } = default!;

    // Bulk action variables
    private bool isBulkDelete = false;
    private string deleteModalDescription = string.Empty;

    // Form and data variables
    private StatusDto statusModel = new() { ColorCode = "#3b82f6" };
    private string? successMessage;
    private bool isError = false;

    // Edit mode variables
    private bool isEditing = false;
    private int? editingStatusId = null;

    // Delete modal variables
    private bool showDeleteModal = false;
    private StatusDto? statusToDelete;

    protected bool? selectedFilterStatus = null;

    // Filter statuses based on search term
    private IEnumerable<StatusDto> FilteredStatuses =>
        (Facade.State.Statuses ?? Enumerable.Empty<StatusDto>())
        .Where(s => string.IsNullOrWhiteSpace(Facade.State.SearchTerm) || s.Name.Contains(Facade.State.SearchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(s => selectedFilterStatus == null || s.IsActive == selectedFilterStatus);

    protected override async Task OnInitializedAsync()
    {
        Facade.State.OnStateChange += StateHasChanged;
        await Facade.LoadStatusesAsync();
    }

    public void Dispose()
    {
        Facade.State.OnStateChange -= StateHasChanged;
    }

    private async Task HandleSubmitStatus()
    {
        if (string.IsNullOrWhiteSpace(statusModel.Name)) return;
        isError = false;

        if (isEditing && editingStatusId.HasValue)
        {
            statusModel.Id = editingStatusId.Value;
            await Facade.UpdateAsync(statusModel);
            successMessage = "وضعیت با موفقیت ویرایش شد.";
        }
        else
        {
            statusModel.ColorCode = string.IsNullOrEmpty(statusModel.ColorCode) ? "#3b82f6" : statusModel.ColorCode;
            await Facade.AddAsync(statusModel);
            successMessage = "وضعیت با موفقیت ایجاد شد.";
        }

        CancelEdit();
        await Facade.LoadStatusesAsync();
        _ = Task.Delay(3000).ContinueWith(_ => { successMessage = null; InvokeAsync(StateHasChanged); });
    }

    private void EditStatus(StatusDto status)
    {
        isEditing = true;
        editingStatusId = status.Id;
        statusModel = status.Adapt<StatusDto>();
    }

    private void CancelEdit()
    {
        isEditing = false;
        editingStatusId = null;
        statusModel = new StatusDto { ColorCode = "#3b82f6", NeedApproval = false };
        successMessage = null;
    }

    private void HandleSearch(string term)
    {
        Facade.State.SearchTerm = term;
    }

    private void OnSelectionChanged(HashSet<int> newKeys)
    {
        Facade.State.SelectedStatusIds = newKeys;
    }

    private void ClearSelection()
    {
        Facade.State.SelectedStatusIds.Clear();
    }

    private void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        deleteModalDescription = $"آیا از حذف {Facade.State.SelectedStatusIds.Count} وضعیت انتخاب شده مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        showDeleteModal = true;
    }

    private void OpenDeleteModal(StatusDto status)
    {
        statusToDelete = status;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف وضعیت «{status.Name}» مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        showDeleteModal = true;
    }

    private void CancelDelete()
    {
        showDeleteModal = false;
        statusToDelete = null;
        isBulkDelete = false;
    }

    private async Task ConfirmDelete()
    {
        try
        {
            if (isBulkDelete)
            {
                await Facade.DeleteRangeAsync(Facade.State.SelectedStatusIds);
                var verb = Facade.State.SelectedStatusIds.Count == 1 ? "شد" : "شدند";
                successMessage = $"{Facade.State.SelectedStatusIds.Count} وضعیت با موفقیت حذف {verb}.";
                ClearSelection();
            }
            else if (statusToDelete != null)
            {
                await Facade.DeleteAsync(statusToDelete.Id);
                successMessage = "وضعیت با موفقیت حذف شد.";
                Facade.State.SelectedStatusIds.Remove(statusToDelete.Id);
                if (isEditing && editingStatusId == statusToDelete.Id) CancelEdit();
            }

            isError = false;
            await Facade.LoadStatusesAsync();
        }
        catch (Exception)
        {
            isError = true;
            successMessage = "امکان حذف وجود ندارد! ابتدا باید تیکت‌هایی که در این وضعیت هستند را ویرایش کنید.";
        }
        finally
        {
            CancelDelete();
            _ = Task.Delay(4000).ContinueWith(_ => { successMessage = null; isError = false; InvokeAsync(StateHasChanged); });
        }
    }

    private async Task BulkActivateStatuses() => await UpdateStatusesStatus(true, "فعال");
    private async Task BulkDeactivateStatuses() => await UpdateStatusesStatus(false, "غیرفعال");

    private async Task UpdateStatusesStatus(bool isActive, string actionName)
    {
        try
        {
            await Facade.UpdateStatusRangeAsync(Facade.State.SelectedStatusIds, isActive);
            var count = Facade.State.SelectedStatusIds.Count;
            successMessage = $"{count} وضعیت با موفقیت {actionName} {(count == 1 ? "شد" : "شدند")}.";
            ClearSelection();
        }
        catch (Exception)
        {
            isError = true;
            successMessage = "عملیات با خطا مواجه شد!";
        }
        finally
        {
            _ = Task.Delay(4000).ContinueWith(_ => { successMessage = null; isError = false; InvokeAsync(StateHasChanged); });
        }
    }

    private void FilterByStatus(bool? status)
    {
        selectedFilterStatus = status;
    }
}