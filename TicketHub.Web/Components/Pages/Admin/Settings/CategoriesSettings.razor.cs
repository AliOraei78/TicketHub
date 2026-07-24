using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;

// تغییر به مسیر دقیق بر اساس ارورهای شما
namespace TicketHub.Web.Components.Pages.Admin.Settings;

public partial class CategoriesSettings : ComponentBase
{
    [Inject] public AppDbContext DbContext { get; set; } = default!;

    private HashSet<int> selectedCategoryIds = new();
    private bool isBulkDelete = false;
    private string deleteModalDescription = string.Empty;

    private Category categoryModel = new();
    private List<Category>? categories;
    private string? successMessage;
    private bool isError = false;
    private string searchTerm = string.Empty;

    private bool isEditing = false;
    private int? editingCategoryId = null;

    private bool showDeleteModal = false;
    private Category? categoryToDelete;

    private IEnumerable<Category> FilteredCategories =>
        string.IsNullOrWhiteSpace(searchTerm)
            ? (categories ?? Enumerable.Empty<Category>())
            : (categories ?? Enumerable.Empty<Category>()).Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        categories = await DbContext.Set<Category>().ToListAsync();
    }

    private async Task HandleSubmitCategory()
    {
        if (string.IsNullOrWhiteSpace(categoryModel.Name)) return;

        isError = false;

        if (isEditing && editingCategoryId.HasValue)
        {
            var categoryToUpdate = await DbContext.Set<Category>().FindAsync(editingCategoryId.Value);
            if (categoryToUpdate != null)
            {
                categoryToUpdate.Name = categoryModel.Name;
                successMessage = "نوع تیکت با موفقیت ویرایش شد.";
            }
        }
        else
        {
            categoryModel.CreatedAt = DateTime.UtcNow;
            DbContext.Set<Category>().Add(categoryModel);
            successMessage = "نوع تیکت با موفقیت ایجاد شد.";
        }

        await DbContext.SaveChangesAsync();
        CancelEdit();
        await LoadCategories();

        _ = Task.Delay(3000).ContinueWith(_ => { successMessage = null; InvokeAsync(StateHasChanged); });
    }

    private void EditCategory(Category category)
    {
        isEditing = true;
        editingCategoryId = category.Id;
        categoryModel.Name = category.Name;
    }

    private void CancelEdit()
    {
        isEditing = false;
        editingCategoryId = null;
        categoryModel = new Category();
        successMessage = null;
    }

    private void HandleSearch(string term)
    {
        searchTerm = term;
    }

    private void OnSelectionChanged(HashSet<int> newKeys)
    {
        selectedCategoryIds = newKeys;
    }

    private void ClearSelection()
    {
        selectedCategoryIds.Clear();
    }

    private void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        deleteModalDescription = $"آیا از حذف {selectedCategoryIds.Count} نوع تیکت انتخاب شده مطمئن هستید؟";
        showDeleteModal = true;
    }

    private void OpenDeleteModal(Category category)
    {
        categoryToDelete = category;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف «{category.Name}» مطمئن هستید؟";
        showDeleteModal = true;
    }

    private void CancelDelete()
    {
        showDeleteModal = false;
        categoryToDelete = null;
        isBulkDelete = false;
    }

    private async Task ConfirmDelete()
    {
        try
        {
            if (isBulkDelete)
            {
                var categoriesToRemove = await DbContext.Set<Category>().Where(c => selectedCategoryIds.Contains(c.Id)).ToListAsync();
                DbContext.Set<Category>().RemoveRange(categoriesToRemove);
                await DbContext.SaveChangesAsync();

                successMessage = $"{categoriesToRemove.Count} آیتم با موفقیت حذف {(categoriesToRemove.Count == 1 ? "شد" : "شدند")}.";
                ClearSelection();
            }
            else if (categoryToDelete != null)
            {
                DbContext.Set<Category>().Remove(categoryToDelete);
                await DbContext.SaveChangesAsync();

                successMessage = "نوع تیکت با موفقیت حذف شد.";
                if (isEditing && editingCategoryId == categoryToDelete.Id) CancelEdit();
            }

            isError = false;
            await LoadCategories();
        }
        catch (Exception)
        {
            isError = true;
            successMessage = "امکان حذف وجود ندارد! تیکت‌های مرتبط را بررسی کنید.";
        }
        finally
        {
            CancelDelete();
            _ = Task.Delay(4000).ContinueWith(_ => { successMessage = null; isError = false; InvokeAsync(StateHasChanged); });
        }
    }
}