using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Categories;

public partial class CategoryForm : ComponentBase
{
    [Parameter] public CategoryDto Model { get; set; } = new();
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public IEnumerable<ProjectDto> AvailableProjects { get; set; } = new List<ProjectDto>();
}
