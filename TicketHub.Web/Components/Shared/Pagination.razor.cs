using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class Pagination : ComponentBase
{
    [Parameter] public int CurrentPage { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public int TotalItems { get; set; }
    [Parameter] public string ItemName { get; set; } = "رکورد";
    [Parameter] public EventCallback OnNext { get; set; }
    [Parameter] public EventCallback OnPrevious { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;

    protected int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
