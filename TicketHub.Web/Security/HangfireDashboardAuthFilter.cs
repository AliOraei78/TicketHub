using System.Linq;
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
            // Allow admin users
            return httpContext.User.IsInRole("Admin") || 
                   httpContext.User.Claims.Any(c => c.Value == "Admin" || c.Value.Contains("Admin"));
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
