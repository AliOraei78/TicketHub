using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TicketHub.Application.Common.Models;

namespace TicketHub.Web.Security;

public static class SsoAuthenticationExtensions
{
    public static IServiceCollection AddTicketHubAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var ssoSection = configuration.GetSection(SsoSettings.SectionName);
        services.Configure<SsoSettings>(ssoSection);
        var ssoSettings = ssoSection.Get<SsoSettings>() ?? new SsoSettings();

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "TicketHub_Session";
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/access-denied";

            // --- تنظیمات امنیتی استاندارد ---
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;

            // --- تنظیمات انقضا ---
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        if (ssoSettings.Enabled && !string.IsNullOrWhiteSpace(ssoSettings.Authority) && !string.IsNullOrWhiteSpace(ssoSettings.ClientId))
        {
            authBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = ssoSettings.Authority;
                options.ClientId = ssoSettings.ClientId;
                options.ClientSecret = ssoSettings.ClientSecret;
                options.ResponseType = ssoSettings.ResponseType;
                options.RequireHttpsMetadata = ssoSettings.RequireHttpsMetadata;
                options.CallbackPath = ssoSettings.CallbackPath;
                options.SignedOutCallbackPath = ssoSettings.SignedOutCallbackPath;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                options.Scope.Clear();
                foreach (var scope in ssoSettings.Scopes)
                {
                    options.Scope.Add(scope);
                }

                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });
        }

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }
}
