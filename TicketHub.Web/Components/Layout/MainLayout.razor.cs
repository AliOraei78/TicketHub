using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common;
using TicketHub.Web.Components.Layout.Shared;

namespace TicketHub.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IUserService UserService { get; set; } = default!;

    protected bool IsMobileMenuOpen { get; set; } = false;
    protected bool IsProfileMenuOpen { get; set; } = false;
    protected bool IsSidebarCollapsed { get; set; } = false;
    protected string CurrentPersianDate { get; set; } = string.Empty;

    protected string CurrentUserName { get; set; } = "کاربر سیستم";
    protected string CurrentUserEmail { get; set; } = string.Empty;
    protected string PrimaryRoleName { get; set; } = "کاربر";
    protected string PrimaryRoleColorClass { get; set; } = "text-cyan-400";
    protected List<string> UserRoles { get; set; } = new();

    protected CustomErrorBoundary? ErrorBoundary { get; set; }

    protected void ToggleMobileMenu() => IsMobileMenuOpen = !IsMobileMenuOpen;
    protected void ToggleProfileMenu() => IsProfileMenuOpen = !IsProfileMenuOpen;
    protected void ToggleDesktopSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;
    protected void OpenSidebar() => IsSidebarCollapsed = false;

    protected override async Task OnInitializedAsync()
    {
        CurrentPersianDate = DateTime.UtcNow.ToPersianDateString();

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var nameClaim = user.Identity.Name;
                var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var emailClaim = user.FindFirst(ClaimTypes.Email)?.Value;

                if (!string.IsNullOrWhiteSpace(nameClaim)) CurrentUserName = nameClaim;
                if (!string.IsNullOrWhiteSpace(emailClaim)) CurrentUserEmail = emailClaim;

                if (int.TryParse(userIdStr, out var id))
                {
                    var userDto = await UserService.GetByIdAsync(id);
                    if (userDto != null)
                    {
                        CurrentUserName = string.IsNullOrWhiteSpace(userDto.Name) ? CurrentUserName : userDto.Name;
                        CurrentUserEmail = userDto.Email;
                        UserRoles = userDto.RoleNames ?? new();
                    }
                }

                if (!UserRoles.Any())
                {
                    var roleClaims = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                    if (roleClaims.Any()) UserRoles = roleClaims;
                }

                SetPrimaryRole();
            }
        }
        catch { }
    }

    private void SetPrimaryRole()
    {
        if (!UserRoles.Any())
        {
            PrimaryRoleName = "کاربر عادی";
            PrimaryRoleColorClass = "text-emerald-400";
            return;
        }

        var adminRole = UserRoles.FirstOrDefault(r =>
            r.Contains("مدیر") ||
            r.Contains("ادمین") ||
            r.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Administrator", StringComparison.OrdinalIgnoreCase));
        if (adminRole != null)
        {
            PrimaryRoleName = adminRole;
            PrimaryRoleColorClass = "text-purple-400";
            return;
        }

        var expertRole = UserRoles.FirstOrDefault(r =>
            r.Contains("کارشناس") ||
            r.Contains("فنی") ||
            r.Contains("Expert", StringComparison.OrdinalIgnoreCase));
        if (expertRole != null)
        {
            PrimaryRoleName = expertRole;
            PrimaryRoleColorClass = "text-amber-400";
            return;
        }

        var supportRole = UserRoles.FirstOrDefault(r =>
            r.Contains("پشتیبان") ||
            r.Contains("Support", StringComparison.OrdinalIgnoreCase));
        if (supportRole != null)
        {
            PrimaryRoleName = supportRole;
            PrimaryRoleColorClass = "text-cyan-400";
            return;
        }

        PrimaryRoleName = UserRoles.First();
        PrimaryRoleColorClass = "text-emerald-400";
    }

    protected override void OnParametersSet()
    {
        ErrorBoundary?.Recover();
    }
}

