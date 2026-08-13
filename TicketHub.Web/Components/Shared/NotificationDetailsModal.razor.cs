using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Core.Common;
using TicketHub.Core.Enums;

namespace TicketHub.Web.Components.Shared;

public partial class NotificationDetailsModal : ComponentBase
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public NotificationDto? Notification { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<int> OnDelete { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;

    protected async Task CloseModal()
    {
        await OnClose.InvokeAsync();
    }

    protected async Task DeleteNotification()
    {
        if (Notification != null)
        {
            await OnDelete.InvokeAsync(Notification.Id);
            await CloseModal();
        }
    }

    protected async Task NavigateToAction()
    {
        if (Notification != null && !string.IsNullOrWhiteSpace(Notification.ActionUrl))
        {
            var url = Notification.ActionUrl;
            await CloseModal();
            Navigation.NavigateTo(url);
        }
    }

    protected string GetSeverityBadgeClass(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Success => "bg-emerald-950/80 text-emerald-300 border border-emerald-500/40 shadow-[0_0_10px_rgba(16,185,129,0.2)]",
        NotificationSeverity.Warning => "bg-amber-950/80 text-amber-300 border border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.2)]",
        NotificationSeverity.Danger => "bg-rose-950/80 text-rose-300 border border-rose-500/40 shadow-[0_0_10px_rgba(244,63,94,0.2)]",
        _ => "bg-cyan-950/80 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(56,189,248,0.2)]"
    };

    protected string GetSeverityBoxClass(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Success => "bg-emerald-950/90 text-emerald-400 border border-emerald-500/60 shadow-[0_0_25px_rgba(16,185,129,0.35)]",
        NotificationSeverity.Warning => "bg-amber-950/90 text-amber-400 border border-amber-500/60 shadow-[0_0_25px_rgba(245,158,11,0.35)]",
        NotificationSeverity.Danger => "bg-rose-950/90 text-rose-400 border border-rose-500/60 shadow-[0_0_25px_rgba(244,63,94,0.35)]",
        _ => "bg-cyan-950/90 text-cyan-400 border border-cyan-500/60 shadow-[0_0_25px_rgba(56,189,248,0.35)]"
    };

    protected string GetSeverityCornerClass(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Success => "border-emerald-400",
        NotificationSeverity.Warning => "border-amber-400",
        NotificationSeverity.Danger => "border-rose-400",
        _ => "border-cyan-400"
    };

    protected RenderFragment GetSeverityIconLarge(NotificationSeverity severity) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "w-6 h-6 drop-shadow-[0_0_8px_currentColor]");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "stroke", "currentColor");
        builder.AddAttribute(4, "viewBox", "0 0 24 24");
        builder.OpenElement(5, "path");
        builder.AddAttribute(6, "stroke-linecap", "round");
        builder.AddAttribute(7, "stroke-linejoin", "round");
        builder.AddAttribute(8, "stroke-width", "2");

        var d = severity switch
        {
            NotificationSeverity.Success => "M5 13l4 4L19 7",
            NotificationSeverity.Warning => "M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z",
            NotificationSeverity.Danger => "M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z",
            _ => "M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        };
        builder.AddAttribute(9, "d", d);
        builder.CloseElement();
        builder.CloseElement();
    };

    protected string GetTypeName(NotificationType type) => type switch
    {
        NotificationType.TicketComment => "پاسخ جدید",
        NotificationType.StatusChanged => "تغییر وضعیت",
        NotificationType.TicketAssigned => "ارجاع تیکت",
        NotificationType.TicketCreated => "تیکت جدید",
        NotificationType.DeadlineApproaching => "نزدیک به مهلت اقدام",
        NotificationType.DeadlineBreached => "سررسید مهلت اقدام",
        _ => "سیستمی"
    };
}
