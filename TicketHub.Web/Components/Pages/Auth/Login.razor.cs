using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Interfaces;

namespace TicketHub.Web.Components.Pages.Auth;

public partial class Login : ComponentBase
{
    [Inject] protected IUserService UserService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IEmailService EmailService { get; set; } = default!;

    [SupplyParameterFromForm]
    public LoginViewModel loginModel { get; set; } = new();

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    protected string? errorMessage;
    protected bool isLoading = false;

    protected async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            if (HttpContext == null)
            {
                errorMessage = "خطای ارتباط با سرور رخ داد.";
                return;
            }

            var result = await UserService.LoginAsync(loginModel);

            if (!result.Success)
            {
                if (result.RequiresConfirmation)
                {
                    Navigation.NavigateTo($"/confirm-email?email={result.Email}");
                    return;
                }
                errorMessage = result.ErrorMessage;
                return;
            }

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new Claim(ClaimTypes.Name, result.Name),
            new Claim(ClaimTypes.Email, result.Email!)
        };

            foreach (var role in result.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

            Navigation.NavigateTo("/", true);
        }
        finally
        {
            isLoading = false;
        }
    }
}
