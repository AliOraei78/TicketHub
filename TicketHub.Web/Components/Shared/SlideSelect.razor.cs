using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TicketHub.Web.Components.Shared;

public partial class SlideSelect<TItem, TValue> : ComponentBase
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string Placeholder { get; set; } = "انتخاب کنید...";
    [Parameter] public IEnumerable<TItem> Items { get; set; } = default!;
    [Parameter] public Func<TItem, string> TextSelector { get; set; } = default!;
    [Parameter] public Func<TItem, TValue> ValueSelector { get; set; } = default!;

    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter] public bool AllowClear { get; set; } = true;
    [Parameter] public string ClearText { get; set; } = "بدون انتخاب";

    protected bool IsOpen { get; set; }
    protected string SearchTerm { get; set; } = string.Empty;
    protected string ElementId { get; set; } = "slide-select-" + Guid.NewGuid().ToString("N");

    protected IEnumerable<TItem> FilteredItems
    {
        get
        {
            if (Items == null) return Enumerable.Empty<TItem>();
            if (string.IsNullOrEmpty(SearchTerm)) return Items;
            return Items.Where(i => TextSelector(i)?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true);
        }
    }

    protected async Task Toggle()
    {
        IsOpen = !IsOpen;
        if (!IsOpen)
        {
            SearchTerm = string.Empty;
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("initClickOutside", ElementId, DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public void CloseDropdown()
    {
        IsOpen = false;
        SearchTerm = string.Empty;
        StateHasChanged();
    }

    protected async Task Select(TValue val)
    {
        Value = val;
        IsOpen = false;
        await ValueChanged.InvokeAsync(val);
    }

    protected string GetSelectedText()
    {
        if (EqualityComparer<TValue>.Default.Equals(Value, default) || Items == null)
            return Placeholder;

        var selectedItem = Items.FirstOrDefault(x => EqualityComparer<TValue>.Default.Equals(ValueSelector(x), Value));
        return selectedItem != null ? TextSelector(selectedItem) : Placeholder;
    }
}
