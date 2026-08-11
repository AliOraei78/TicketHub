using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class CustomInput : ComponentBase
{
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public string Type { get; set; } = "text";
    [Parameter] public string Placeholder { get; set; } = string.Empty;
    [Parameter] public string Dir { get; set; } = "rtl";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;

    protected string CurrentValue
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
