using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class EntityManagerTemplate<TItem> : ComponentBase
{
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }
    [Parameter] public RenderFragment? ToolbarTemplate { get; set; }
    [Parameter] public RenderFragment? ListTemplate { get; set; }
    [Parameter] public RenderFragment? PaginationTemplate { get; set; }
    [Parameter] public RenderFragment? ModalTemplate { get; set; }

    [Parameter] public IEnumerable<TItem>? Items { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;
}
