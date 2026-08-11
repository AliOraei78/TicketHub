using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class CustomCheckbox : ComponentBase
{
    [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public bool Value { get; set; }
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;

    protected bool CurrentValue
    {
        get => Value;
        set
        {
            if (Value != value)
            {
                Value = value;
                ValueChanged.InvokeAsync(value);
            }
        }
    }
}
