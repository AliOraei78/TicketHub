using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class ListFilterToolbar : ComponentBase
{
    [Parameter] public string SearchTerm { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SearchTermChanged { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public int PageSize { get; set; } = 10;
    [Parameter] public EventCallback<int> PageSizeChanged { get; set; }
    [Parameter] public string SearchPlaceholder { get; set; } = "جستجو...";
    [Parameter] public string ItemName { get; set; } = "آیتم";
    [Parameter] public List<int> PageSizeOptions { get; set; } = new() { 10, 25, 50, 100 };
    [Parameter] public string Class { get; set; } = string.Empty;

    protected async Task OnSearchInputChanged(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? string.Empty;
        await SearchTermChanged.InvokeAsync(value);
    }

    protected async Task OnPageSizeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int newSize))
        {
            await PageSizeChanged.InvokeAsync(newSize);
        }
    }
}
