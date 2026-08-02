// Priorities.razor.cs
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;
using TicketHub.Web.Facades;
using TicketHub.Web.States;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Priorities;

public partial class PrioritiesSettings : ComponentBase, IDisposable
{
    [Inject] public PriorityFacade Facade { get; set; } = default!;
    [Inject] public PriorityState State { get; set; } = default!;

    protected HashSet<int> selectedIds = new();
    protected bool isBulkDelete = false;
    protected string deleteModalDescription = string.Empty;

    protected PriorityDto priorityModel = new() { ColorCode = "#6B7280", Level = 1 };

    protected string searchTerm = string.Empty;
    protected bool isEditing = false;
    protected int? editingId = null;

    protected bool? selectedFilterStatus = null;

    protected bool showDeleteModal = false;
    protected PriorityDto? itemToDelete;

    // خواندن داده‌ها از State
    protected IEnumerable<PriorityDto> FilteredPriorities =>
        (State.Priorities ?? Enumerable.Empty<PriorityDto>())
        .Where(p => string.IsNullOrWhiteSpace(searchTerm) || p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(p => selectedFilterStatus == null || p.IsActive == selectedFilterStatus);

    protected override void OnInitialized()
    {
        State.OnChange += StateHasChanged; // متصل کردن تغییرات State به رندر صفحه
    }

    public void Dispose()
    {
        State.OnChange -= StateHasChanged; // پاکسازی
    }

    protected override async Task OnInitializedAsync() => await Facade.LoadPrioritiesAsync();

    protected async Task HandleSubmit()
    {
        if (string.IsNullOrWhiteSpace(priorityModel.Name)) return;

        if (!isEditing && string.IsNullOrEmpty(priorityModel.ColorCode))
            priorityModel.ColorCode = "#6B7280";

        await Facade.SavePriorityAsync(priorityModel, isEditing);
        CancelEdit();
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

    protected void HandleSearch(string term) => searchTerm = term;
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

    protected async Task ConfirmDelete()
    {
        if (isBulkDelete)
        {
            await Facade.DeleteBulkAsync(selectedIds);
            ClearSelection();
        }
        else if (itemToDelete != null)
        {
            await Facade.DeletePriorityAsync(itemToDelete);
            selectedIds.Remove(itemToDelete.Id);
            if (isEditing && editingId == itemToDelete.Id) CancelEdit();
        }
        CancelDelete();
    }

    protected async Task BulkActivatePriorities() => await UpdatePrioritiesStatus(true);
    protected async Task BulkDeactivatePriorities() => await UpdatePrioritiesStatus(false);

    private async Task UpdatePrioritiesStatus(bool isActive)
    {
        try
        {
            await Facade.UpdateStatusRangeAsync(selectedIds, isActive);
            ClearSelection();
        }
        catch (Exception)
        {
            State.SetMessage("عملیات با خطا مواجه شد!", true);
            _ = Task.Delay(4000).ContinueWith(_ => State.SetMessage(null));
        }
    }

    protected void FilterByStatus(bool? status)
    {
        selectedFilterStatus = status;
        // NotifyStateChanged در State معمولا صدا زده می‌شود
    }
}