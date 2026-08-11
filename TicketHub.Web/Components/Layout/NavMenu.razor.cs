using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Layout;

public partial class NavMenu : ComponentBase
{
    [Parameter] public bool IsMobileMenuOpen { get; set; }
    [Parameter] public EventCallback CloseMobileMenu { get; set; }

    [Parameter] public bool IsCollapsed { get; set; }
    [Parameter] public EventCallback OnExpandRequested { get; set; }
    [Parameter] public EventCallback OnToggleSidebar { get; set; }

    protected async Task HandleSideBarClick()
    {
        if (IsMobileMenuOpen) await CloseMobileMenu.InvokeAsync();
    }

    protected async Task HandleToggleSidebar()
    {
        await OnToggleSidebar.InvokeAsync();
    }
}
