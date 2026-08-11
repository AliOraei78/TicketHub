using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.Settings.TicketFields;

public partial class TicketFieldForm : ComponentBase
{
    [Parameter] public TicketFieldDto Model { get; set; } = new();
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Parameter] public IEnumerable<CategoryDto> AvailableCategories { get; set; } = new List<CategoryDto>();
    [Parameter] public IEnumerable<FieldTypeDto> AvailableFieldTypes { get; set; } = new List<FieldTypeDto>();
}
