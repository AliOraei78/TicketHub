using Fluxor;
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Admin.Settings.TicketFields;

public partial class TicketFieldsSettings
{
    [Inject] public IState<TicketFieldState> FieldState { get; set; } = default!;

    // فرض بر این است که استیت‌های دسته‌بندی و انواع فیلد را برای دراپ‌داون‌ها در سیستم دارید
    [Inject] public IState<CategoryState> CatState { get; set; } = default!;
    [Inject] public IState<FieldTypeState> TypeState { get; set; } = default!;

    [Inject] public IDispatcher Dispatcher { get; set; } = default!;

    private HashSet<int> selectedFieldIds = new();
    private bool isBulkDelete = false;
    private string deleteModalDescription = string.Empty;

    private TicketFieldDto fieldModel = new();
    private bool isEditing = false;
    private int? editingFieldId = null;

    private bool showDeleteModal = false;
    private TicketFieldDto? fieldToDelete;

    private IEnumerable<TicketFieldDto> FilteredFields =>
        FieldState.Value.TicketFields
            .Where(f => string.IsNullOrWhiteSpace(FieldState.Value.SearchTerm) || f.Name.Contains(FieldState.Value.SearchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(f => FieldState.Value.SelectedFilterStatus == null || f.IsActive == FieldState.Value.SelectedFilterStatus)
            .Where(f => !FieldState.Value.SelectedFilterCategoryIds.Any() || (f.CategoryIds != null && f.CategoryIds.Any(c => FieldState.Value.SelectedFilterCategoryIds.Contains(c))))
            .Where(f => !FieldState.Value.SelectedFilterFieldTypeIds.Any() || (f.FieldTypeId != 0 && FieldState.Value.SelectedFilterFieldTypeIds.Contains(f.FieldTypeId)));

    protected override void OnInitialized()
    {
        base.OnInitialized();
        FieldState.StateChanged += OnFirstStateLoaded;
        Dispatcher.Dispatch(new LoadTicketFieldInitialDataAction());
    }

    private async void OnFirstStateLoaded(object? sender, EventArgs e)
    {
        if (!FieldState.Value.IsLoading)
        {
            FieldState.StateChanged -= OnFirstStateLoaded;

            // فراخوانی اول
            Dispatcher.Dispatch(new LoadFieldTypesAction());

            // ایجاد یک وقفه کوتاه برای آزادسازی Thread دیتابیس و جلوگیری از تداخل
            await Task.Delay(150);

            // فراخوانی دوم پس از اتمام قبلی
            Dispatcher.Dispatch(new LoadCategoriesAction());
        }
    }

    private void HandleSubmitField()
    {
        if (string.IsNullOrWhiteSpace(fieldModel.Name)) return;

        if (isEditing && editingFieldId.HasValue)
            fieldModel.Id = editingFieldId.Value;

        Dispatcher.Dispatch(new SaveTicketFieldAction(fieldModel, isEditing));
        CancelEdit();
        ClearMessageAfterDelay();
    }

    private void EditField(TicketFieldDto field)
    {
        isEditing = true;
        editingFieldId = field.Id;
        fieldModel = field.Adapt<TicketFieldDto>();
    }

    private void CancelEdit()
    {
        isEditing = false;
        editingFieldId = null;
        fieldModel = new TicketFieldDto();
    }

    private void HandleSearch(string term) => Dispatcher.Dispatch(new SetTicketFieldSearchAction(term));
    private void FilterByStatus(bool? status) => Dispatcher.Dispatch(new SetTicketFieldFilterStatusAction(status));
    private void FilterByCategories(List<int> categoryIds) => Dispatcher.Dispatch(new SetTicketFieldCategoryFilterAction(categoryIds));

    // استفاده از SingleSelectDropdown برای فیلتر، یک آیدی برمی‌گرداند. آن را به لیست تبدیل می‌کنیم تا با State همخوانی داشته باشد.
    private void FilterByType(List<int> typeIds)
            => Dispatcher.Dispatch(new SetTicketFieldTypeFilterAction(typeIds));

    private void OnSelectionChanged(HashSet<int> newKeys) => selectedFieldIds = newKeys;
    private void ClearSelection() => selectedFieldIds.Clear();

    private void OpenBulkDeleteModal()
    {
        isBulkDelete = true;
        deleteModalDescription = $"آیا از حذف {selectedFieldIds.Count} فیلد انتخاب شده مطمئن هستید؟";
        showDeleteModal = true;
    }

    private void OpenDeleteModal(TicketFieldDto field)
    {
        fieldToDelete = field;
        isBulkDelete = false;
        deleteModalDescription = $"آیا از حذف «{field.Name}» مطمئن هستید؟";
        showDeleteModal = true;
    }

    private void CancelDelete()
    {
        showDeleteModal = false;
        fieldToDelete = null;
        isBulkDelete = false;
    }

    private void ConfirmDelete()
    {
        if (isBulkDelete)
        {
            // اضافه شدن .ToList() برای جلوگیری از پاک شدن رفرنس
            Dispatcher.Dispatch(new DeleteMultipleTicketFieldsAction(selectedFieldIds.ToList()));
            ClearSelection();
        }
        else if (fieldToDelete != null)
        {
            Dispatcher.Dispatch(new DeleteTicketFieldAction(fieldToDelete.Id));
            selectedFieldIds.Remove(fieldToDelete.Id);
            if (isEditing && editingFieldId == fieldToDelete.Id) CancelEdit();
        }

        CancelDelete();
        ClearMessageAfterDelay();
    }

    private void BulkActivateFields() => UpdateFieldsStatus(true);
    private void BulkDeactivateFields() => UpdateFieldsStatus(false);

    private void UpdateFieldsStatus(bool isActive)
    {
        // اضافه شدن .ToList() برای جلوگیری از پاک شدن رفرنس
        Dispatcher.Dispatch(new UpdateTicketFieldStatusAction(selectedFieldIds.ToList(), isActive));
        ClearSelection();
        ClearMessageAfterDelay();
    }

    private void ClearMessageAfterDelay()
    {
        _ = Task.Delay(4000).ContinueWith(_ =>
        {
            Dispatcher.Dispatch(new ClearTicketFieldMessageAction());
        });
    }
}