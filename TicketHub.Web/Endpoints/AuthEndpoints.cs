using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketHub.Application.Common.Models;
using TicketHub.Application.Interfaces;
using TicketHub.Web.Security;

namespace TicketHub.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        // 1. ورود یکپارچه سازمانی (SSO Challenge)
        group.MapGet("/sso-login", (HttpContext context, IOptions<SsoSettings> ssoOptions, ILogger<Program> logger) =>
        {
            var ssoSettings = ssoOptions.Value;
            if (!ssoSettings.Enabled || string.IsNullOrWhiteSpace(ssoSettings.Authority))
            {
                logger.LogWarning("درخواست ورود یکپارچه (SSO) ارسال شد اما SSO در پیکربندی فعال نشده است.");
                return Results.Redirect("/login?error=sso_disabled");
            }

            var props = new AuthenticationProperties
            {
                RedirectUri = "/auth/sso-callback"
            };

            return Results.Challenge(props, new[] { OpenIdConnectDefaults.AuthenticationScheme });
        }).AllowAnonymous();

        // 2. کال‌بک بازگشتی از ارائه‌دهنده هویت (SSO Callback)
        group.MapGet("/sso-callback", async (
            HttpContext context,
            IUserService userService,
            IOptions<SsoSettings> ssoOptions,
            ILogger<Program> logger) =>
        {
            try
            {
                var ssoSettings = ssoOptions.Value;
                var user = context.User;

                if (user?.Identity?.IsAuthenticated != true)
                {
                    logger.LogWarning("کال‌بک SSO فراخوانی شد اما کاربر احراز هویت نشده است.");
                    return Results.Redirect("/login?error=sso_not_authenticated");
                }

                // استخراج ادعاهای استاندارد هویت
                var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("sub")?.Value
                             ?? user.FindFirst("oid")?.Value;

                var email = user.FindFirst(ClaimTypes.Email)?.Value
                         ?? user.FindFirst("email")?.Value
                         ?? user.FindFirst("upn")?.Value;

                var name = user.FindFirst(ClaimTypes.Name)?.Value
                        ?? user.FindFirst("name")?.Value
                        ?? user.FindFirst("preferred_username")?.Value
                        ?? email?.Split('@')[0]
                        ?? "کاربر سازمانی";

                if (string.IsNullOrWhiteSpace(email))
                {
                    logger.LogWarning("در کال‌بک SSO ادعای ایمیل کاربر یافت نشد.");
                    return Results.Redirect("/login?error=sso_missing_email");
                }

                var authResult = await userService.ProcessExternalLoginAsync(
                    ssoSettings.ProviderName,
                    subjectId ?? email,
                    email,
                    name);

                if (!authResult.Success)
                {
                    logger.LogWarning("ورود SSO ناموفق بود: {ErrorMessage}", authResult.ErrorMessage);
                    return Results.Redirect($"/login?error=sso_failed&message={Uri.EscapeDataString(authResult.ErrorMessage ?? "خطا در احراز هویت سازمانی")}");
                }

                // ایجاد ClaimsIdentity کوکی تیکت‌هاب
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, authResult.UserId.ToString()),
                    new Claim(ClaimTypes.Name, authResult.Name),
                    new Claim(ClaimTypes.Email, authResult.Email ?? email),
                    new Claim("auth_provider", ssoSettings.ProviderName)
                };

                foreach (var role in authResult.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });

                logger.LogInformation("کاربر سازمانی {Email} از طریق {Provider} با شناسه {UserId} با موفقیت وارد شد.", email, ssoSettings.ProviderName, authResult.UserId);
                return Results.Redirect("/");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "خطای پیش‌بینی‌نشده در پردازش کال‌بک ورود سازمانی (SSO)");
                return Results.Redirect("/login?error=sso_exception");
            }
        }).AllowAnonymous();

        return app;
    }
}
