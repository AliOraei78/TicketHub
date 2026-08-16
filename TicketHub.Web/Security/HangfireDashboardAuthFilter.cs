using System.Linq;
using System.Security.Claims;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace TicketHub.Web.Security;

public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var user = httpContext.User;
            // Allow admin users (support Persian 'ادمین', 'مدیر' and English 'Admin', 'Administrator')
            var isAdmin = user.IsInRole("Admin") ||
                          user.IsInRole("ادمین") ||
                          user.IsInRole("مدیر") ||
                          user.Claims.Any(c =>
                              (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role")) &&
                              (c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                               c.Value.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                               c.Value.Contains("ادمین") ||
                               c.Value.Contains("مدیر")));

            if (isAdmin)
            {
                return true;
            }
        }

        // Allow localhost connections for development access
        var remoteIp = httpContext?.Connection?.RemoteIpAddress;
        if (remoteIp != null && (remoteIp.Equals(System.Net.IPAddress.Loopback) || remoteIp.Equals(System.Net.IPAddress.IPv6Loopback)))
        {
            return true;
        }

        return false;
    }
}
