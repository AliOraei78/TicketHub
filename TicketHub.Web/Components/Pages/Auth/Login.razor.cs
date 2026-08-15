using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Interfaces;
using DNTCaptcha.Core;

namespace TicketHub.Web.Components.Pages.Auth;

public partial class Login : ComponentBase
{
    [Inject] protected IUserService UserService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IEmailService EmailService { get; set; } = default!;
    [Inject] protected ILogger<Login> Logger { get; set; } = default!;
    [Inject] protected IDNTCaptchaValidatorService CaptchaValidator { get; set; } = default!;

#pragma warning disable BL0008
    [SupplyParameterFromForm]
    public LoginViewModel loginModel { get; set; } = new();
#pragma warning restore BL0008

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

            // --- اعتبارسنجی کپچا ---
            bool isValidCaptcha = CaptchaValidator.HasRequestValidCaptchaEntry();
            if (!isValidCaptcha)
            {
                errorMessage = "کد امنیتی نامعتبر است یا منقضی شده است.";
                isLoading = false;
                return;
            }
            // -----------------------

            var result = await UserService.LoginAsync(loginModel);

            if (!result.Success)
            {
                if (result.RequiresConfirmation)
                {
                    Logger.LogInformation("ورود {Email} نیازمند تایید ایمیل است. انتقال به صفحه تایید.", loginModel.Email);

                    HttpContext.Response.Cookies.Append("TempEmail", result.Email!, new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.UtcNow.AddMinutes(15) });

                    Navigation.NavigateTo("/confirm-email");
                    return;
                }
                Logger.LogWarning("ورود ناموفق {Email} - علت: {ErrorMessage}", loginModel.Email, result.ErrorMessage);
                errorMessage = result.ErrorMessage;
                return;
            }

            Logger.LogInformation("کاربر {Email} با موفقیت وارد سیستم شد.", loginModel.Email);

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
                IsPersistent = loginModel.RememberMe,
                ExpiresUtc = loginModel.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            });

            Navigation.NavigateTo("/", true);
        }
        catch (NavigationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "خطای سرور هنگام ورود کاربر {Email}", loginModel.Email);
            errorMessage = "خطای ارتباط با سرور رخ داد.";
        }
        finally
        {
            isLoading = false;
        }
    }
}