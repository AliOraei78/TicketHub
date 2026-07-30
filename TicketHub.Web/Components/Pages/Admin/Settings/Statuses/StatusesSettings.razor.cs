using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using Mapster;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Statuses;

public partial class StatusesSettings : ComponentBase
{
    [Inject]
    private IStatusService StatusService { get; set; } = default!;

    // Bulk action variables
    private HashSet<int> selectedStatusIds = new();
    private bool isBulkDelete = false;
    private string deleteModalDescription = string.Empty;

    // Form and data variables
    private StatusDto statusModel = new() { ColorCode = "#3b82f6" };
    private List<StatusDto>? statuses;
    private string? successMessage;
    private bool isError = false;

    // Search variables
    private string searchTerm = string.Empty;

    // Edit mode variables
    private bool isEditing = false;
    private int? editingStatusId = null;

    // Delete modal variables
    private bool showDeleteModal = false;
    private StatusDto? statusToDelete;

    // Filter statuses based on search term
    private IEnumerable<StatusDto> FilteredStatuses =>
            string.IsNullOrWhiteSpace(searchTerm)
                ? (statuses ?? Enumerable.Empty<StatusDto>())
                : (statuses ?? Enumerable.Empty<StatusDto>()).Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadStatuses();
    }

    private async Task LoadStatuses()
    {
        statuses = await StatusService.GetAllAsync();
    }

    private async Task HandleSubmitStatus()
    {
        if (string.IsNullOrWhiteSpace(statusModel.Name)) return;
        isError = false;

        if (isEditing && editingStatusId.HasValue)
        {
            statusModel.Id = editingStatusId.Value;
            await StatusService.UpdateAsync(statusModel);
            successMessage = "وضعیت با موفقیت ویرایش شد.";
        }
        else
        {
            statusModel.ColorCode = string.IsNullOrEmpty(statusModel.ColorCode) ? "#3b82f6" : statusModel.ColorCode;
            await StatusService.AddAsync(statusModel);
            successMessage = "وضعیت با موفقیت ایجاد شد.";
        }

        CancelEdit();
        await LoadStatuses();
        _ = Task.Delay(3000).ContinueWith(_ => { successMessage = null; InvokeAsync(StateHasChanged); });
    }

    private void EditStatus(StatusDto status)
    {
        isEditing = true;
        editingStatusId = status.Id;
        // استفاده از Mapster برای کلون کردن آبجکت جهت جلوگیری از تغییر مستقیم رفرنس
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
        searchTerm = term;
    }

    private void OnSelectionChanged(HashSet<int> newKeys)
    {
        selectedStatusIds = newKeys;
    }

    private void ClearSelection()
    {
        selectedStatusIds.Clear();
    }

    private void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        deleteModalDescription = $"آیا از حذف {selectedStatusIds.Count} وضعیت انتخاب شده مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        showDeleteModal = true;
    }

    // Open delete modal for a single item
    private void OpenDeleteModal(StatusDto status)
    {
        statusToDelete = status;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف وضعیت «{status.Name}» مطمئن هستید؟ این عملیات غیرقابل بازگشت است.";
        showDeleteModal = true;
    }

    // Close delete modal
    private void CancelDelete()
    {
        showDeleteModal = false;
        statusToDelete = null;
        isBulkDelete = false;
    }

    // Confirm and execute deletion (both single and bulk)
    private async Task ConfirmDelete()
    {
        try
        {
            if (isBulkDelete)
            {
                await StatusService.DeleteRangeAsync(selectedStatusIds);
                var verb = selectedStatusIds.Count == 1 ? "شد" : "شدند";
                successMessage = $"{selectedStatusIds.Count} وضعیت با موفقیت حذف {verb}.";
                ClearSelection();
            }
            else if (statusToDelete != null)
            {
                await StatusService.DeleteAsync(statusToDelete.Id);
                successMessage = "وضعیت با موفقیت حذف شد.";
                selectedStatusIds.Remove(statusToDelete.Id);
                if (isEditing && editingStatusId == statusToDelete.Id) CancelEdit();
            }

            isError = false;
            await LoadStatuses();
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
}