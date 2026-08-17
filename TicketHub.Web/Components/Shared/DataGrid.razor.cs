using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class DataGrid<TItem, TKey> : ComponentBase
{
    [Parameter] public IEnumerable<TItem> Items { get; set; } = Array.Empty<TItem>();
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public RenderFragment HeaderTemplate { get; set; } = default!;
    [Parameter] public RenderFragment<TItem> RowTemplate { get; set; } = default!;
    [Parameter] public Func<TItem, TKey> KeySelector { get; set; } = default!;
    [Parameter] public Func<TItem, string>? RowClassSelector { get; set; }

    [Parameter] public string EmptyStateValue { get; set; } = "رکوردی";
    [Parameter] public bool IsCyberpunk { get; set; } = false;

    [Parameter] public bool ShowSelection { get; set; } = true;
    [Parameter] public bool ShowRowIndex { get; set; } = true;
    [Parameter] public int CurrentPage { get; set; } = 1;
    [Parameter] public int PageSize { get; set; } = 10;
    [Parameter] public string Class { get; set; } = string.Empty;
    [Parameter] public string TableMinWidth { get; set; } = string.Empty;

    [Parameter] public HashSet<TKey> SelectedKeys { get; set; } = new();
    [Parameter] public EventCallback<HashSet<TKey>> SelectedKeysChanged { get; set; }

    protected bool IsAllSelected => Items.Any() && Items.All(i => SelectedKeys.Contains(KeySelector(i)));

    protected async Task ToggleAll(ChangeEventArgs e)
    {
        bool isChecked = (bool)(e.Value ?? false);
        var newSet = new HashSet<TKey>(SelectedKeys);
        if (isChecked)
        {
            foreach (var item in Items) newSet.Add(KeySelector(item));
        }
        else
        {
            foreach (var item in Items) newSet.Remove(KeySelector(item));
        }

        SelectedKeys = newSet;
        await SelectedKeysChanged.InvokeAsync(newSet);
    }

    protected async Task ToggleSelection(TKey key, object? value)
    {
        bool isChecked = (bool)(value ?? false);
        var newSet = new HashSet<TKey>(SelectedKeys);
        if (isChecked) newSet.Add(key);
        else newSet.Remove(key);

        SelectedKeys = newSet;
        await SelectedKeysChanged.InvokeAsync(newSet);
    }
}
