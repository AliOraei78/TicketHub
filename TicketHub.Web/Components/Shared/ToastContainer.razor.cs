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
        ToastType.Success => "toast-cyber-success",
        ToastType.Error => "toast-cyber-error",
        ToastType.Warning => "toast-cyber-warning",
        _ => "toast-cyber-info"
    };

    protected string GetBadgeText(ToastType type) => type switch
    {
        ToastType.Success => "[SYS // SUCCESS]",
        ToastType.Error => "[SYS // ALARM_ERROR]",
        ToastType.Warning => "[SYS // WARNING]",
        _ => "[SYS // SYSTEM_INFO]"
    };

    public void Dispose()
    {
        ToastService.OnChanged -= StateHasChangedWrapper;
    }
}
