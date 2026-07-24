// Priorities.razor.cs
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Web.Pages.Admin.Settings;

public partial class PrioritiesSettings : ComponentBase
{
    [Inject] public AppDbContext DbContext { get; set; } = default!;

    protected HashSet<int> selectedIds = new();
    protected bool isBulkDelete = false;
    protected string deleteModalDescription = string.Empty;

    protected Priority priorityModel = new() { ColorCode = "#6B7280", Level = 1 };
    protected List<Priority>? priorities;
    protected string? successMessage;
    protected bool isError = false;

    protected string searchTerm = string.Empty;
    protected bool isEditing = false;
    protected int? editingId = null;

    protected bool showDeleteModal = false;
    protected Priority? itemToDelete;

    protected IEnumerable<Priority> FilteredPriorities =>
        string.IsNullOrWhiteSpace(searchTerm)
            ? (priorities ?? Enumerable.Empty<Priority>())
            : (priorities ?? Enumerable.Empty<Priority>()).Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync() => await LoadData();

    private async Task LoadData() => priorities = await DbContext.Priorities.ToListAsync();

    protected async Task HandleSubmit()
    {
        if (string.IsNullOrWhiteSpace(priorityModel.Name)) return;
        isError = false;

        if (isEditing && editingId.HasValue)
        {
            var item = await DbContext.Priorities.FindAsync(editingId.Value);
            if (item != null)
            {
                item.Name = priorityModel.Name;
                item.ColorCode = priorityModel.ColorCode ?? "#6B7280";
                item.Level = priorityModel.Level;
                successMessage = "اولویت با موفقیت ویرایش شد.";
            }
        }
        else
        {
            if (string.IsNullOrEmpty(priorityModel.ColorCode)) priorityModel.ColorCode = "#6B7280";
            DbContext.Priorities.Add(priorityModel);
            successMessage = "اولویت با موفقیت ایجاد شد.";
        }

        await DbContext.SaveChangesAsync();
        CancelEdit();
        await LoadData();
        _ = Task.Delay(3000).ContinueWith(_ => { successMessage = null; InvokeAsync(StateHasChanged); });
    }

    protected void EditPriority(Priority item)
    {
        isEditing = true;
        editingId = item.Id;
        priorityModel.Name = item.Name;
        priorityModel.ColorCode = item.ColorCode ?? "#6B7280";
        priorityModel.Level = item.Level;
    }

    protected void CancelEdit()
    {
        isEditing = false;
        editingId = null;
        priorityModel = new Priority { ColorCode = "#6B7280", Level = 1 };
        successMessage = null;
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

    protected void OpenDeleteModal(Priority item)
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
        try
        {
            if (isBulkDelete)
            {
                var items = await DbContext.Priorities.Where(p => selectedIds.Contains(p.Id)).ToListAsync();
                DbContext.Priorities.RemoveRange(items);
                await DbContext.SaveChangesAsync();
                successMessage = $"{items.Count} اولویت با موفقیت حذف شدند.";
                ClearSelection();
            }
            else if (itemToDelete != null)
            {
                DbContext.Priorities.Remove(itemToDelete);
                await DbContext.SaveChangesAsync();
                successMessage = "اولویت با موفقیت حذف شد.";
                if (isEditing && editingId == itemToDelete.Id) CancelEdit();
            }
            isError = false;
            await LoadData();
        }
        catch
        {
            isError = true;
            successMessage = "امکان حذف وجود ندارد! ابتدا باید تیکت‌های مرتبط با این اولویت را ویرایش کنید.";
        }
        finally
        {
            CancelDelete();
            _ = Task.Delay(4000).ContinueWith(_ => { successMessage = null; isError = false; InvokeAsync(StateHasChanged); });
        }
    }
}