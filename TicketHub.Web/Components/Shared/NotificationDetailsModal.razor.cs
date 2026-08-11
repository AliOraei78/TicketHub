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
        NotificationSeverity.Success => "bg-emerald-50 text-emerald-700 border border-emerald-200",
        NotificationSeverity.Warning => "bg-amber-50 text-amber-700 border border-amber-200",
        NotificationSeverity.Danger => "bg-rose-50 text-rose-700 border border-rose-200",
        _ => "bg-indigo-50 text-indigo-700 border border-indigo-200"
    };

    protected RenderFragment GetSeverityIcon(NotificationSeverity severity) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "w-3.5 h-3.5");
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
