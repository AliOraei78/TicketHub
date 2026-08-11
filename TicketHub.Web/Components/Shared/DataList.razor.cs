using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class DataList<TItem> : ComponentBase
{
    [Parameter] public IEnumerable<TItem>? Items { get; set; }
    [Parameter] public RenderFragment<TItem> ItemTemplate { get; set; } = default!;
    [Parameter] public string EmptyMessage { get; set; } = "اطلاعاتی یافت نشد.";
    [Parameter] public string ContainerClass { get; set; } = "grid grid-cols-1 gap-4";
}
