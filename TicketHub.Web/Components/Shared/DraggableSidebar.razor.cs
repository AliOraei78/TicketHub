using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class DraggableSidebar<TItem> : ComponentBase
{
    [Parameter] public string Title { get; set; } = "آیتم‌ها";
    [Parameter] public IEnumerable<TItem> Items { get; set; } = default!;
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public RenderFragment<TItem> ItemTemplate { get; set; } = default!;
    [Parameter] public EventCallback<TItem> OnDragStart { get; set; }
    [Parameter] public EventCallback<TItem> OnItemAdd { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;
}
