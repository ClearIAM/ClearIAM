using CleanIAM.Scopes.Core;
using OpenIddict.Abstractions;

namespace CleanIAM.Scopes;

public static class ScopesConstants
{
    /// <summary>
    /// List of CleanIAM default scope names.
    /// </summary>
    public static readonly string[] DefaultScopeNames =
    [
        OpenIddictConstants.Scopes.OpenId,
        OpenIddictConstants.Scopes.Profile,
        OpenIddictConstants.Scopes.Email,
        OpenIddictConstants.Scopes.Roles,
        OpenIddictConstants.Scopes.OfflineAccess
    ];

    /// <summary>
    /// List of CleanIAM default scopes.
    /// </summary>
    public static readonly Scope[] DefaultScopes =
    [
        new()
        {
            Name = OpenIddictConstants.Scopes.OpenId,
            DisplayName = "OpenId",
            Description = "Access to your openid",
            Resources = ["management-portal"]
        },
        new()
        {
            Name = OpenIddictConstants.Scopes.Profile,
            DisplayName = "Profile",
            Description = "Access to your profile",
            Resources = ["management-portal"]
        },
        new()
        {
            Name = OpenIddictConstants.Scopes.Email,
            DisplayName = "Email",
            Description = "Access to your email",
            Resources = ["management-portal"]
        },
        new()
        {
            Name = OpenIddictConstants.Scopes.Roles,
            DisplayName = "Roles",
            Description = "Access to your roles",
            Resources = ["management-portal"]
        },
        new()
        {
            Name = OpenIddictConstants.Scopes.OfflineAccess,
            DisplayName = "Offline Access",
            Description = "Allow refresh token usage",
            Resources = ["management-portal"]
        }
    ];
}