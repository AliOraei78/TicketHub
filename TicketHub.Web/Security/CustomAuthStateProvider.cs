using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace TicketHub.Web.Security;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _localStorage;
    private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthStateProvider(ProtectedLocalStorage localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Retrieve session from local storage
            var userSessionResult = await _localStorage.GetAsync<string>("user_session");
            var userSession = userSessionResult.Success ? userSessionResult.Value : null;

            if (string.IsNullOrEmpty(userSession))
                return new AuthenticationState(_anonymous);

            // Split stored string: Id|Name|Role
            var parts = userSession.Split('|');
            if (parts.Length != 3)
                return new AuthenticationState(_anonymous);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, parts[0]),
                new Claim(ClaimTypes.Name, parts[1]),
                new Claim(ClaimTypes.Role, parts[2])
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch
        {
            // Return anonymous state if JS interop fails (e.g., during pre-rendering)
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task UpdateAuthenticationState(string? userSession)
    {
        ClaimsPrincipal principal;

        if (!string.IsNullOrEmpty(userSession))
        {
            await _localStorage.SetAsync("user_session", userSession);

            var parts = userSession.Split('|');
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, parts[0]),
                new Claim(ClaimTypes.Name, parts[1]),
                new Claim(ClaimTypes.Role, parts[2])
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            principal = new ClaimsPrincipal(identity);
        }
        else
        {
            // Clear session on logout
            await _localStorage.DeleteAsync("user_session");
            principal = _anonymous;
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }
}