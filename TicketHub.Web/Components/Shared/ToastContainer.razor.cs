using Microsoft.AspNetCore.Components;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Components.Shared;

public partial class ToastContainer : ComponentBase, IDisposable
{
    [Inject] public IToastService ToastService { get; set; } = default!;

    [Parameter] public string Class { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        ToastService.OnChanged += StateHasChangedWrapper;
    }

    private void StateHasChangedWrapper()
    {
        InvokeAsync(StateHasChanged);
    }

    protected string GetCssClass(ToastType type) => type switch
    {
        ToastType.Success => "bg-emerald-50 border-emerald-200 text-emerald-800",
        ToastType.Error => "bg-rose-50 border-rose-200 text-rose-800",
        ToastType.Warning => "bg-amber-50 border-amber-200 text-amber-800",
        _ => "bg-indigo-50 border-indigo-200 text-indigo-800"
    };

    public void Dispose()
    {
        ToastService.OnChanged -= StateHasChangedWrapper;
    }
}
