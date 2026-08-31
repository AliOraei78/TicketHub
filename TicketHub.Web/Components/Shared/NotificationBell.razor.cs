using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common;
using TicketHub.Core.Enums;

namespace TicketHub.Web.Components.Shared;

public partial class NotificationBell : ComponentBase, IDisposable
{
    [Inject] public INotificationService NotificationService { get; set; } = default!;
    [Inject] public ITicketEventBroker EventBroker { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public string Class { get; set; } = string.Empty;

    protected int CurrentUserId { get; set; } = 0;
    protected int UnreadCount { get; set; } = 0;
    protected bool IsDropdownOpen { get; set; } = false;
    protected bool IsLoading { get; set; } = false;
    protected bool FilterUnreadOnly { get; set; } = false;
    protected List<NotificationDto> Notifications { get; set; } = new();

    protected bool IsModalOpen { get; set; } = false;
    protected NotificationDto? SelectedNotification { get; set; }

    protected IEnumerable<NotificationDto> FilteredNotifications =>
        FilterUnreadOnly ? Notifications.Where(n => !n.IsRead) : Notifications;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            if (int.TryParse(userIdStr, out var id))
            {
                CurrentUserId = id;
                await LoadInitialUnreadCountAsync();
            }
        }

        EventBroker.OnNotificationReceived += HandleNotificationReceivedAsync;
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    private void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        if (IsDropdownOpen)
        {
            IsDropdownOpen = false;
            InvokeAsync(StateHasChanged);
        }
    }

    protected async Task LoadInitialUnreadCountAsync()
    {
        if (CurrentUserId > 0)
        {
            try
            {
                UnreadCount = await NotificationService.GetUnreadCountAsync(CurrentUserId);
            }
            catch
            {
                UnreadCount = 0;
            }
        }
    }

    protected async Task LoadNotificationsListAsync()
    {
        if (CurrentUserId <= 0) return;

        IsLoading = true;
        try
        {
            var summary = await NotificationService.GetSummaryAsync(CurrentUserId, limit: 30);
            UnreadCount = summary.UnreadCount;
            Notifications = summary.RecentNotifications;
        }
        catch
        {
            // Suppress background errors safely
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task ToggleDropdown()
    {
        IsDropdownOpen = !IsDropdownOpen;
        if (IsDropdownOpen)
        {
            await LoadNotificationsListAsync();
        }
    }

    protected void CloseDropdown()
    {
        IsDropdownOpen = false;
    }

    protected void SetFilter(bool unreadOnly)
    {
        FilterUnreadOnly = unreadOnly;
    }

    protected async Task MarkAllAsRead()
    {
        if (CurrentUserId <= 0) return;

        await NotificationService.MarkAllAsReadAsync(CurrentUserId);
        UnreadCount = 0;
        foreach (var n in Notifications)
        {
            n.IsRead = true;
        }
        StateHasChanged();
    }

    protected async Task OpenNotificationDetails(NotificationDto notif)
    {
        SelectedNotification = notif;
        IsModalOpen = true;
        IsDropdownOpen = false;

        if (!notif.IsRead && CurrentUserId > 0)
        {
            notif.IsRead = true;
            UnreadCount = Math.Max(0, UnreadCount - 1);
            await NotificationService.MarkAsReadAsync(notif.Id, CurrentUserId);
        }
    }

    protected void CloseDetailsModal()
    {
        IsModalOpen = false;
        SelectedNotification = null;
    }

    protected async Task HandleDeleteNotification(int notificationId)
    {
        if (CurrentUserId <= 0) return;

        try
        {
            await NotificationService.DeleteNotificationAsync(notificationId, CurrentUserId);
            var item = Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (item != null)
            {
                if (!item.IsRead)
                {
                    UnreadCount = Math.Max(0, UnreadCount - 1);
                }
                Notifications.Remove(item);
            }
            ToastService.ShowSuccess("اعلان با موفقیت حذف شد.");
        }
        catch (Exception ex)
        {
            ToastService.ShowError("خطا در حذف اعلان: " + ex.Message);
        }
    }

    private async Task HandleNotificationReceivedAsync(int targetUserId, NotificationDto notification)
    {
        if (targetUserId == CurrentUserId && CurrentUserId > 0)
        {
            await InvokeAsync(async () =>
            {
                UnreadCount++;
                Notifications.Insert(0, notification);
                if (Notifications.Count > 30)
                {
                    Notifications.RemoveAt(Notifications.Count - 1);
                }

                await PlayChimeSoundAsync();
                StateHasChanged();
            });
        }
    }

    private async Task PlayChimeSoundAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("playCyberSound", "create");
        }
        catch
        {
            // Fallback
        }
    }

    protected string GetTimeAgo(DateTime createdAt)
    {
        var span = DateTime.UtcNow - createdAt;
        if (span.TotalMinutes < 1) return "هم‌اکنون";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} دقیقه پیش";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} ساعت پیش";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} روز پیش";
        return createdAt.ToPersianDateString();
    }

    protected string GetSeverityIconContainerClass(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Success => "bg-emerald-950/80 text-emerald-400 border border-emerald-500/40 shadow-[0_0_10px_rgba(16,185,129,0.25)]",
        NotificationSeverity.Warning => "bg-amber-950/80 text-amber-400 border border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.25)]",
        NotificationSeverity.Danger => "bg-rose-950/80 text-rose-400 border border-rose-500/40 shadow-[0_0_10px_rgba(244,63,94,0.25)]",
        _ => "bg-cyan-950/80 text-cyan-400 border border-cyan-500/40 shadow-[0_0_10px_rgba(56,189,248,0.25)]"
    };

    protected RenderFragment GetSeverityIcon(NotificationSeverity severity) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "w-4 h-4");
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

    public void Dispose()
    {
        EventBroker.OnNotificationReceived -= HandleNotificationReceivedAsync;
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }
}
