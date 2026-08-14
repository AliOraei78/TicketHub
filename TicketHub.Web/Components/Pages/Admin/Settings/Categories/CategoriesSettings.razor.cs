using Fluxor;
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Categories;

public partial class CategoriesSettings
{
    [Inject] public IState<CategoryState> CatState { get; set; } = default!;
    [Inject] public IState<ProjectState> ProjState { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;

    private HashSet<int> selectedCategoryIds = new();
    private bool isBulkDelete = false;
    private string deleteModalDescription = string.Empty;

    private CategoryDto categoryModel = new();
    private bool isEditing = false;
    private int? editingCategoryId = null;

    private bool showDeleteModal = false;
    private CategoryDto? categoryToDelete;

    // Telemetry Statistics
    private int TotalCategoriesCount => CatState.Value.Categories?.Count() ?? 0;
    private int ActiveCategoriesCount => CatState.Value.Categories?.Count(c => c.IsActive) ?? 0;
    private int InactiveCategoriesCount => CatState.Value.Categories?.Count(c => !c.IsActive) ?? 0;

    private IEnumerable<CategoryDto> FilteredCategories =>
        CatState.Value.Categories
            .Where(c => string.IsNullOrWhiteSpace(CatState.Value.SearchTerm) || c.Name.Contains(CatState.Value.SearchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(c => CatState.Value.SelectedFilterStatus == null || c.IsActive == CatState.Value.SelectedFilterStatus)
            .Where(c => !CatState.Value.SelectedFilterProjectIds.Any() || (c.ProjectIds != null && c.ProjectIds.Any(p => CatState.Value.SelectedFilterProjectIds.Contains(p))));

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Dispatcher.Dispatch(new LoadCategoryInitialDataAction());
    }

    private void HandleSubmitCategory()
    {
        if (string.IsNullOrWhiteSpace(categoryModel.Name)) return;

        if (isEditing && editingCategoryId.HasValue)
            categoryModel.Id = editingCategoryId.Value;

        Dispatcher.Dispatch(new SaveCategoryAction(categoryModel, isEditing));
        CancelEdit();
    }

    private void EditCategory(CategoryDto category)
    {
        isEditing = true;
        editingCategoryId = category.Id;
        categoryModel = category.Adapt<CategoryDto>();
    }

    private void CancelEdit()
    {
        isEditing = false;
        editingCategoryId = null;
        categoryModel = new CategoryDto();
    }

    private void HandleSearch(string term) => Dispatcher.Dispatch(new SetCategorySearchAction(term));
    private void FilterByStatus(bool? status) => Dispatcher.Dispatch(new SetCategoryFilterStatusAction(status));
    private void FilterByProjects(List<int> projectIds) => Dispatcher.Dispatch(new SetCategoryProjectFilterAction(projectIds));

    private void OnSelectionChanged(HashSet<int> newKeys) => selectedCategoryIds = new HashSet<int>(newKeys);
    private void ClearSelection() => selectedCategoryIds = new HashSet<int>();

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

    private void ConfirmDelete()
    {
        if (isBulkDelete)
        {
            // استفاده از ToList برای ارسال یک کپی از مقادیر و جلوگیری از صفر شدن به دلیل ClearSelection
            Dispatcher.Dispatch(new DeleteMultipleCategoriesAction(selectedCategoryIds.ToList()));
            ClearSelection();
        }
        else if (categoryToDelete != null)
        {
            Dispatcher.Dispatch(new DeleteCategoryAction(categoryToDelete.Id));

            var newSelection = new HashSet<int>(selectedCategoryIds);
            newSelection.Remove(categoryToDelete.Id);
            selectedCategoryIds = newSelection;

            if (isEditing && editingCategoryId == categoryToDelete.Id) CancelEdit();
        }

        CancelDelete();
    }

    private void BulkActivateCategories() => UpdateCategoriesStatus(true);
    private void BulkDeactivateCategories() => UpdateCategoriesStatus(false);

    private void UpdateCategoriesStatus(bool isActive)
    {
        Dispatcher.Dispatch(new UpdateCategoryStatusAction(selectedCategoryIds.ToList(), isActive));
        ClearSelection();
    }
}