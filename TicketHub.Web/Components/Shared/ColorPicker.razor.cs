using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class ColorPicker : ComponentBase
{
    [Parameter] public string Value { get; set; } = "#3b82f6";
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public string Placeholder { get; set; } = string.Empty;
    [Parameter] public string Class { get; set; } = string.Empty;

    protected async Task OnColorChanged(ChangeEventArgs e)
    {
        var newColor = e.Value?.ToString() ?? "#3b82f6";
        await ValueChanged.InvokeAsync(newColor);
    }
}
