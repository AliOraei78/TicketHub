using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TicketHub.Web.Components.Shared;

public partial class CustomPersianDatePicker : ComponentBase, IDisposable
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public string Placeholder { get; set; } = string.Empty;
    [Parameter] public bool ShowTimePicker { get; set; } = false;
    [Parameter] public string Class { get; set; } = string.Empty;

    protected string InputId { get; set; } = $"dp_{Guid.NewGuid():N}";
    private DotNetObjectReference<CustomPersianDatePicker>? _objRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _objRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("initJalaliDatePicker", InputId, ShowTimePicker, _objRef);
        }
    }

    [JSInvokable]
    public async Task UpdateValue(string newValue)
    {
        if (Value != newValue)
        {
            Value = newValue;
            await ValueChanged.InvokeAsync(newValue);
            StateHasChanged();
        }
    }

    protected async Task HandleManualChange(ChangeEventArgs e)
    {
        var val = e.Value?.ToString() ?? string.Empty;
        await UpdateValue(val);
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }
}
