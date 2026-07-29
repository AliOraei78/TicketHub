using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;
using TicketHub.Web.Facades;
using TicketHub.Web.State;

namespace TicketHub.Web.Components.Pages.Admin.Settings;

public partial class CategoriesSettings : ComponentBase, IDisposable
{
    // اینجکت کردن Facade و State به جای سرویس مستقیم
    [Inject] public CategoryFacade CategoryFacade { get; set; } = default!;
    [Inject] public CategoryState categoryState { get; set; } = default!;

    private HashSet<int> selectedCategoryIds = new();
    private bool isBulkDelete = false;
    private string deleteModalDescription = string.Empty;

    private CategoryDto categoryModel = new();
    private string? successMessage;
    private bool isError = false;
    private string searchTerm = string.Empty;

    private bool isEditing = false;
    // دیگر نیازی به نگه‌داری ID به صورت جداگانه نیست چون مدل خودش ID دارد
    // اما برای کنترل لاجیک فرم بد نیست نگهش داریم
    private int? editingCategoryId = null;

    private bool showDeleteModal = false;
    private CategoryDto? categoryToDelete;

    // فیلتر کردن از State خوانده می‌شود
    private IEnumerable<CategoryDto> FilteredCategories =>
        string.IsNullOrWhiteSpace(searchTerm)
            ? categoryState.Categories
            : categoryState.Categories.Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        // ساب‌اسکرایب به تغییرات State
        categoryState.OnChange += StateHasChanged;

        await CategoryFacade.LoadCategoriesAsync();
    }

    private async Task HandleSubmitCategory()
    {
        if (string.IsNullOrWhiteSpace(categoryModel.Name)) return;

        isError = false;

        // اگر در حال ویرایش هستیم، مطمئن شویم آیدی درست ست شده
        if (isEditing && editingCategoryId.HasValue)
        {
            categoryModel.Id = editingCategoryId.Value;
        }

        try
        {
            await CategoryFacade.AddOrUpdateAsync(categoryModel, isEditing);
            successMessage = isEditing ? "نوع تیکت با موفقیت ویرایش شد." : "نوع تیکت با موفقیت ایجاد شد.";
            CancelEdit();
        }
        catch (Exception)
        {
            isError = true;
            successMessage = "خطایی در ذخیره اطلاعات رخ داد.";
        }

        _ = Task.Delay(3000).ContinueWith(_ => { successMessage = null; InvokeAsync(StateHasChanged); });
    }

    private void EditCategory(CategoryDto category)
    {
        isEditing = true;
        editingCategoryId = category.Id;

        // ایجاد یک کپی جدید تا تغییرات موقت مستقیما روی استیت اعمال نشود
        categoryModel = new CategoryDto { Id = category.Id, Name = category.Name };
    }

    private void CancelEdit()
    {
        isEditing = false;
        editingCategoryId = null;
        categoryModel = new CategoryDto();
        successMessage = null;
        isError = false;
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

    private void OpenDeleteModal(CategoryDto category)
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
                await CategoryFacade.DeleteRangeAsync(selectedCategoryIds);
                successMessage = $"{selectedCategoryIds.Count} آیتم با موفقیت حذف {(selectedCategoryIds.Count == 1 ? "شد" : "شدند")}.";
                ClearSelection();
            }
            else if (categoryToDelete != null)
            {
                await CategoryFacade.DeleteAsync(categoryToDelete);
                successMessage = "نوع تیکت با موفقیت حذف شد.";
                selectedCategoryIds.Remove(categoryToDelete.Id);
                if (isEditing && editingCategoryId == categoryToDelete.Id) CancelEdit();
            }

            isError = false;
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

    public void Dispose()
    {
        // آنساب‌اسکرایب برای جلوگیری از مموری لیک
        categoryState.OnChange -= StateHasChanged;
    }
}