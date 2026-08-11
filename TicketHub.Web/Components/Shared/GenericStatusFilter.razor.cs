using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class GenericStatusFilter<TValue> : ComponentBase
{
    [Parameter, EditorRequired] public IEnumerable<TValue> Options { get; set; } = default!;
    [Parameter] public TValue SelectedValue { get; set; } = default!;
    [Parameter] public EventCallback<TValue> OnValueChanged { get; set; }

    [Parameter, EditorRequired] public RenderFragment<TValue> ItemTemplate { get; set; } = default!;
    [Parameter] public Func<TValue, string> BackgroundClassSelector { get; set; } = _ => "";
    [Parameter] public Func<TValue, TValue, string> ItemClassSelector { get; set; } = (_, __) => "";
    [Parameter] public string Class { get; set; } = string.Empty;
}
