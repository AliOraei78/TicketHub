using System.Collections.Generic;

namespace TicketHub.Application.Common.Models;

public class SsoSettings
{
    public const string SectionName = "SSO";

    /// <summary>
    /// Whether Single Sign-On (OIDC / OAuth2) is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Provider identifier (e.g. "OIDC", "Keycloak", "Google", "Microsoft", "Okta")
    /// </summary>
    public string ProviderName { get; set; } = "OIDC";

    /// <summary>
    /// UI display title for the SSO button
    /// </summary>
    public string DisplayName { get; set; } = "ورود یکپارچه سازمانی (SSO)";

    /// <summary>
    /// OIDC Authority / Issuer Discovery URL (e.g. "https://keycloak.company.com/realms/master" or "https://accounts.google.com")
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Client ID configured on the Identity Provider
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret configured on the Identity Provider
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Response type (default: "code")
    /// </summary>
    public string ResponseType { get; set; } = "code";

    /// <summary>
    /// Whether to require HTTPS for OIDC metadata (can be disabled in local dev/testing)
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = false;

    /// <summary>
    /// Callback path for receiving authentication code from IdP
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Signed out callback path
    /// </summary>
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

    /// <summary>
    /// OIDC scopes requested
    /// </summary>
    public List<string> Scopes { get; set; } = new() { "openid", "profile", "email" };

    /// <summary>
    /// Default role assigned to auto-provisioned users
    /// </summary>
    public string DefaultRole { get; set; } = "کاربر";

    /// <summary>
    /// Whether to automatically create a user record if they authenticate successfully via SSO but don't exist yet
    /// </summary>
    public bool AutoProvisionUsers { get; set; } = true;
}
