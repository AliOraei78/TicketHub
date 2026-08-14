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

    // Telemetry Statistics
    private int TotalFieldsCount => FieldState.Value.TicketFields?.Count() ?? 0;
    private int ActiveFieldsCount => FieldState.Value.TicketFields?.Count(f => f.IsActive) ?? 0;
    private int InactiveFieldsCount => FieldState.Value.TicketFields?.Count(f => !f.IsActive) ?? 0;

    private IEnumerable<TicketFieldDto> FilteredFields =>
        FieldState.Value.TicketFields
            .Where(f => string.IsNullOrWhiteSpace(FieldState.Value.SearchTerm) || f.Name.Contains(FieldState.Value.SearchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(f => FieldState.Value.SelectedFilterStatus == null || f.IsActive == FieldState.Value.SelectedFilterStatus)
            .Where(f => !FieldState.Value.SelectedFilterCategoryIds.Any() || (f.CategoryIds != null && f.CategoryIds.Any(c => FieldState.Value.SelectedFilterCategoryIds.Contains(c))))
            .Where(f => !FieldState.Value.SelectedFilterFieldTypeIds.Any() || (f.FieldTypeId != 0 && FieldState.Value.SelectedFilterFieldTypeIds.Contains(f.FieldTypeId)));

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Dispatcher.Dispatch(new LoadTicketFieldInitialDataAction());
        Dispatcher.Dispatch(new LoadFieldTypesAction());
        Dispatcher.Dispatch(new LoadCategoriesAction());
    }

    private void HandleSubmitField()
    {

        if (isEditing && editingFieldId.HasValue)
            fieldModel.Id = editingFieldId.Value;

        Dispatcher.Dispatch(new SaveTicketFieldAction(fieldModel, isEditing));
        CancelEdit();
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

    private void OnSelectionChanged(HashSet<int> newKeys) => selectedFieldIds = new HashSet<int>(newKeys);
    private void ClearSelection() => selectedFieldIds = new HashSet<int>();

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
            Dispatcher.Dispatch(new DeleteMultipleTicketFieldsAction(selectedFieldIds.ToList()));
            ClearSelection();
        }
        else if (fieldToDelete != null)
        {
            Dispatcher.Dispatch(new DeleteTicketFieldAction(fieldToDelete.Id));

            // ایجاد یک نمونه جدید برای تریگر شدن StateHasChanged در Blazor
            var newSelection = new HashSet<int>(selectedFieldIds);
            newSelection.Remove(fieldToDelete.Id);
            selectedFieldIds = newSelection;

            if (isEditing && editingFieldId == fieldToDelete.Id) CancelEdit();
        }

        CancelDelete();
    }

    private void BulkActivateFields() => UpdateFieldsStatus(true);
    private void BulkDeactivateFields() => UpdateFieldsStatus(false);

    private void UpdateFieldsStatus(bool isActive)
    {
        Dispatcher.Dispatch(new UpdateTicketFieldStatusAction(selectedFieldIds.ToList(), isActive));
        ClearSelection();
    }

    private string GetFieldTypeAvatarClass(TicketHub.Application.Enums.FieldTypeEnum type) => type switch
    {
        TicketHub.Application.Enums.FieldTypeEnum.Text => "bg-cyan-500/10 border border-cyan-400/25 text-cyan-300 shadow-[0_0_8px_rgba(56,189,248,0.15)]",
        TicketHub.Application.Enums.FieldTypeEnum.TextArea => "bg-teal-500/10 border border-teal-400/25 text-teal-300 shadow-[0_0_8px_rgba(20,184,166,0.15)]",
        TicketHub.Application.Enums.FieldTypeEnum.Number => "bg-blue-500/10 border border-blue-400/25 text-blue-300 shadow-[0_0_8px_rgba(59,130,246,0.15)]",
        TicketHub.Application.Enums.FieldTypeEnum.Date => "bg-amber-500/10 border border-amber-400/25 text-amber-300 shadow-[0_0_8px_rgba(245,158,11,0.15)]",
        TicketHub.Application.Enums.FieldTypeEnum.Dropdown => "bg-purple-500/10 border border-purple-400/25 text-purple-300 shadow-[0_0_8px_rgba(168,85,247,0.15)]",
        TicketHub.Application.Enums.FieldTypeEnum.MultipleDropdown => "bg-indigo-500/10 border border-indigo-400/25 text-indigo-300 shadow-[0_0_8px_rgba(99,102,241,0.15)]",
        TicketHub.Application.Enums.FieldTypeEnum.Checkbox => "bg-emerald-500/10 border border-emerald-400/25 text-emerald-300 shadow-[0_0_8px_rgba(16,185,129,0.15)]",
        TicketHub.Application.Enums.FieldTypeEnum.File => "bg-rose-500/10 border border-rose-400/25 text-rose-300 shadow-[0_0_8px_rgba(244,63,94,0.15)]",
        TicketHub.Application.Enums.FieldTypeEnum.ColorPicker => "bg-pink-500/10 border border-pink-400/25 text-pink-300 shadow-[0_0_8px_rgba(236,72,153,0.15)]",
        _ => "bg-cyan-500/10 border border-cyan-400/25 text-cyan-300 shadow-[0_0_8px_rgba(56,189,248,0.15)]"
    };

    private string GetFieldTypeBadgeClass(TicketHub.Application.Enums.FieldTypeEnum type) => type switch
    {
        TicketHub.Application.Enums.FieldTypeEnum.Text => "bg-cyan-500/10 border-cyan-400/30 text-cyan-300",
        TicketHub.Application.Enums.FieldTypeEnum.TextArea => "bg-teal-500/10 border-teal-400/30 text-teal-300",
        TicketHub.Application.Enums.FieldTypeEnum.Number => "bg-blue-500/10 border-blue-400/30 text-blue-300",
        TicketHub.Application.Enums.FieldTypeEnum.Date => "bg-amber-500/10 border-amber-400/30 text-amber-300",
        TicketHub.Application.Enums.FieldTypeEnum.Dropdown => "bg-purple-500/10 border-purple-400/30 text-purple-300",
        TicketHub.Application.Enums.FieldTypeEnum.MultipleDropdown => "bg-indigo-500/10 border-indigo-400/30 text-indigo-300",
        TicketHub.Application.Enums.FieldTypeEnum.Checkbox => "bg-emerald-500/10 border-emerald-400/30 text-emerald-300",
        TicketHub.Application.Enums.FieldTypeEnum.File => "bg-rose-500/10 border-rose-400/30 text-rose-300",
        TicketHub.Application.Enums.FieldTypeEnum.ColorPicker => "bg-pink-500/10 border-pink-400/30 text-pink-300",
        _ => "bg-purple-500/10 border-purple-400/30 text-purple-300"
    };

    private RenderFragment GetFieldTypeIcon(TicketHub.Application.Enums.FieldTypeEnum type) => builder =>
    {
        var seq = 0;
        builder.OpenElement(seq++, "svg");
        builder.AddAttribute(seq++, "xmlns", "http://www.w3.org/2000/svg");
        builder.AddAttribute(seq++, "class", "h-4 w-4");
        builder.AddAttribute(seq++, "fill", "none");
        builder.AddAttribute(seq++, "viewBox", "0 0 24 24");
        builder.AddAttribute(seq++, "stroke", "currentColor");

        builder.OpenElement(seq++, "path");
        builder.AddAttribute(seq++, "stroke-linecap", "round");
        builder.AddAttribute(seq++, "stroke-linejoin", "round");
        builder.AddAttribute(seq++, "stroke-width", "2");

        var pathD = type switch
        {
            TicketHub.Application.Enums.FieldTypeEnum.Text => "M4 6h16M4 12h8m-8 6h16",
            TicketHub.Application.Enums.FieldTypeEnum.TextArea => "M4 6h16M4 10h16M4 14h16M4 18h10",
            TicketHub.Application.Enums.FieldTypeEnum.Number => "M7 20l4-16m2 16l4-16M6 9h14M4 15h14",
            TicketHub.Application.Enums.FieldTypeEnum.Date => "M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z",
            TicketHub.Application.Enums.FieldTypeEnum.Dropdown => "M8 9l4-4 4 4m0 6l-4 4-4-4",
            TicketHub.Application.Enums.FieldTypeEnum.MultipleDropdown => "M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4",
            TicketHub.Application.Enums.FieldTypeEnum.Checkbox => "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z",
            TicketHub.Application.Enums.FieldTypeEnum.File => "M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13",
            TicketHub.Application.Enums.FieldTypeEnum.ColorPicker => "M7 21a4 4 0 01-4-4 5 5 0 013-4.5V5a2 2 0 012-2h4a2 2 0 012 2v7.5A5 5 0 0117 17a4 4 0 01-4 4H7z",
            _ => "M4 6h16M4 12h16M4 18h7"
        };

        builder.AddAttribute(seq++, "d", pathD);
        builder.CloseElement();
        builder.CloseElement();
    };
}