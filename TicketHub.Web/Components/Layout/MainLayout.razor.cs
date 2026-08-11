using Microsoft.AspNetCore.Components;
using TicketHub.Core.Common;
using TicketHub.Web.Components.Layout.Shared;

namespace TicketHub.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    protected bool IsMobileMenuOpen { get; set; } = false;
    protected bool IsProfileMenuOpen { get; set; } = false;
    protected bool IsSidebarCollapsed { get; set; } = false;
    protected string CurrentPersianDate { get; set; } = string.Empty;

    protected CustomErrorBoundary? ErrorBoundary { get; set; }

    protected void ToggleMobileMenu() => IsMobileMenuOpen = !IsMobileMenuOpen;
    protected void ToggleProfileMenu() => IsProfileMenuOpen = !IsProfileMenuOpen;
    protected void ToggleDesktopSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;
    protected void OpenSidebar() => IsSidebarCollapsed = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        CurrentPersianDate = DateTime.UtcNow.ToPersianDateString();
    }

    protected override void OnParametersSet()
    {
        ErrorBoundary?.Recover();
    }
}
