// TicketHub.Web/Auth/CustomAuthStateProvider.cs
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace TicketHub.Web.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Simulating a logged-in user for development purposes
            // Later, this will read from a real Cookie or JWT token
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "Admin User"),
                new Claim(ClaimTypes.Email, "admin@tickethub.com"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "DevelopmentAuth");

            var user = new ClaimsPrincipal(identity);
            var state = new AuthenticationState(user);

            return Task.FromResult(state);
        }
    }
}