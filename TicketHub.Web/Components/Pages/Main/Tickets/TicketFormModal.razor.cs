using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Main.Tickets;

public partial class TicketFormModal : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter, EditorRequired] public TicketDto Model { get; set; } = default!;
    [Parameter, EditorRequired] public IEnumerable<ProjectDto> Projects { get; set; } = default!;
    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter, EditorRequired] public IEnumerable<PriorityDto> Priorities { get; set; } = default!;
    [Parameter, EditorRequired] public IEnumerable<CategoryDto> Categories { get; set; } = default!;
    [Parameter] public IEnumerable<TicketFieldDto> DynamicFields { get; set; } = Array.Empty<TicketFieldDto>();
    [Parameter] public EventCallback<int?> OnCategoryChanged { get; set; }

    protected IEnumerable<PriorityDto> SortedPriorities => Priorities?.OrderBy(p => p.Level) ?? Enumerable.Empty<PriorityDto>();

    protected List<DynamicFieldModel> DynamicFieldModels { get; set; } = new();
    protected Dictionary<string, string> DynamicValidationErrors { get; set; } = new();

    protected override void OnParametersSet()
    {
        var activeFields = DynamicFields?.Where(f => f.IsActive).ToList() ?? new List<TicketFieldDto>();
        var currentFieldIds = DynamicFieldModels.Select(m => m.OriginalFieldId).ToList();
        var newFieldIds = activeFields.Select(f => f.Id).ToList();

        if (!currentFieldIds.SequenceEqual(newFieldIds))
        {
            DynamicFieldModels = activeFields
                .Select(f => new DynamicFieldModel
                {
                    OriginalFieldId = f.Id,
                    Name = f.Name,
                    Placeholder = f.Placeholder,
                    SortOrder = f.SortOrder,
                    DefaultValue = f.DefaultValue,
                    IsRequired = f.IsRequired,
                    Options = f.Options,
                    FieldTypeId = f.FieldTypeId,
                    Value = Model.FieldValues?.FirstOrDefault(x => x.TicketFieldId == f.Id)?.Value ?? (!string.IsNullOrWhiteSpace(f.DefaultValue) ? f.DefaultValue : string.Empty)
                }).ToList();
        }
    }

    private void MapDynamicFieldsToModel()
    {
        Model.FieldValues = DynamicFieldModels.Select(df => new TicketFieldValueDto
        {
            TicketFieldId = df.OriginalFieldId,
            IsRequired = df.IsRequired,
            FieldName = df.Name,
            Value = df.Value,
            PendingUploads = df.PendingUploads
        }).ToList();
    }

    protected async Task HandleSubmit(EditContext editContext)
    {
        DynamicValidationErrors.Clear();
        MapDynamicFieldsToModel();

        bool isValid = editContext.Validate();

        foreach (var field in DynamicFieldModels)
        {
            if (field.IsRequired)
            {
                bool hasTextValue = !string.IsNullOrWhiteSpace(field.Value);
                bool hasFiles = field.PendingUploads?.Any() == true;
                if (!hasTextValue && !hasFiles)
                {
                    DynamicValidationErrors[field.Name] = $"تکمیل فیلد «{field.Name}» الزامی است.";
                    isValid = false;
                }
            }
        }

        if (isValid)
        {
            if (OnSubmit.HasDelegate)
                await OnSubmit.InvokeAsync();
        }
    }

    protected async Task HandleCancel()
    {
        if (OnCancel.HasDelegate)
            await OnCancel.InvokeAsync();
    }

    protected IEnumerable<CategoryDto> FilteredCategories =>
        Model.ProjectId > 0 && Categories != null
            ? Categories.Where(c => c.ProjectIds != null && c.ProjectIds.Contains(Model.ProjectId))
            : Enumerable.Empty<CategoryDto>();

    protected async Task HandleProjectChange(int projectId)
    {
        Model.ProjectId = projectId;
        if (Model.CategoryId.HasValue && !FilteredCategories.Any(c => c.Id == Model.CategoryId.Value))
        {
            await HandleCategoryChange(null);
        }
    }

    protected async Task HandleCategoryChange(int? categoryId)
    {
        Model.CategoryId = categoryId;
        if (OnCategoryChanged.HasDelegate)
        {
            await OnCategoryChanged.InvokeAsync(categoryId);
        }
    }
}
