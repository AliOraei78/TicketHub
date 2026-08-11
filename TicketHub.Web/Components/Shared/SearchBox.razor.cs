using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class SearchBox : ComponentBase
{
    [Parameter] public string Placeholder { get; set; } = "جستجو...";
    [Parameter] public string SearchTerm { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> OnSearchChanged { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;

    protected async Task OnInputChanged(ChangeEventArgs e)
    {
        SearchTerm = e.Value?.ToString() ?? string.Empty;
        await OnSearchChanged.InvokeAsync(SearchTerm);
    }
}
