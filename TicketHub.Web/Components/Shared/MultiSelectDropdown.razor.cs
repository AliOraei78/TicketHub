using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TicketHub.Web.Components.Shared;

public partial class MultiSelectDropdown<TItem, TValue> : ComponentBase
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string Placeholder { get; set; } = string.Empty;
    [Parameter] public IEnumerable<TItem> Items { get; set; } = Array.Empty<TItem>();
    [Parameter] public List<TValue> SelectedValues { get; set; } = new();
    [Parameter] public EventCallback<List<TValue>> SelectedValuesChanged { get; set; }
    [Parameter] public Func<TItem, TValue> ValueSelector { get; set; } = default!;
    [Parameter] public Func<TItem, string> DisplaySelector { get; set; } = default!;
    [Parameter] public string BadgeBgClass { get; set; } = "bg-indigo-50";
    [Parameter] public string BadgeTextClass { get; set; } = "text-indigo-700";
    [Parameter] public string BadgeButtonClass { get; set; } = "text-indigo-500 hover:text-indigo-900";

    protected string SearchTerm { get; set; } = string.Empty;
    protected bool ShowDropdown { get; set; } = false;
    protected string ElementId { get; set; } = "multi-select-" + Guid.NewGuid().ToString("N");

    protected bool AreAllSelected => FilteredItems.Any() && FilteredItems.All(i => SelectedValues.Contains(ValueSelector(i)));

    protected IEnumerable<TItem> FilteredItems => string.IsNullOrEmpty(SearchTerm)
        ? Items
        : Items.Where(i => DisplaySelector(i).Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected async Task OpenDropdown()
    {
        if (!ShowDropdown)
        {
            ShowDropdown = true;
            await JSRuntime.InvokeVoidAsync("initClickOutside", ElementId, DotNetObjectReference.Create(this));
        }
    }

    protected async Task ToggleDropdown()
    {
        ShowDropdown = !ShowDropdown;
        if (ShowDropdown)
        {
            await JSRuntime.InvokeVoidAsync("initClickOutside", ElementId, DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public void CloseDropdown()
    {
        ShowDropdown = false;
        StateHasChanged();
    }

    protected async Task ToggleSelection(TValue value)
    {
        var newValues = new List<TValue>(SelectedValues);
        if (newValues.Contains(value))
            newValues.Remove(value);
        else
            newValues.Add(value);

        await SelectedValuesChanged.InvokeAsync(newValues);
    }

    protected async Task RemoveSelection(TValue value)
    {
        var newValues = new List<TValue>(SelectedValues);
        newValues.Remove(value);
        await SelectedValuesChanged.InvokeAsync(newValues);
    }

    protected async Task ToggleSelectAll()
    {
        var newValues = new List<TValue>(SelectedValues);
        if (AreAllSelected)
        {
            var filteredValues = FilteredItems.Select(ValueSelector).ToHashSet();
            newValues.RemoveAll(v => filteredValues.Contains(v));
        }
        else
        {
            var valuesToAdd = FilteredItems.Select(ValueSelector).Where(v => !newValues.Contains(v));
            newValues.AddRange(valuesToAdd);
        }

        await SelectedValuesChanged.InvokeAsync(newValues);
    }
}
