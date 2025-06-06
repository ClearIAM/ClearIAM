using System.Text;
using CleanIAM.Identity.Application.Interfaces;
using CleanIAM.Identity.Core.Requests;
using CleanIAM.Identity.Infrastructure.Services;
using CommunityToolkit.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using CleanIAM.SharedKernel.Core.Database;
using Marten;
using Microsoft.AspNetCore.Identity;
using IdentityUser = CleanIAM.Identity.Core.Users.IdentityUser;

namespace CleanIAM.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityProject(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ISigninRequestService, SigninRequestService>();
        services.AddTransient<IPasswordHasher, PasswordHasher>();
        services.AddTransient<IIdentityBuilderService, IdentityBuilderService>();
        services.AddScoped<IEmailService, CoravelEmailService>();
        
        // Register all aggregates to marten document store
        services.ConfigureMarten(opts =>
        {
            opts.Schema.For<IdentityUser>();
            opts.Schema.For<SigninRequest>().SingleTenanted();
            opts.Schema.For<PasswordResetRequest>().SingleTenanted();
            opts.Schema.For<InvitationRequest>().SingleTenanted();
            opts.Schema.For<EmailVerificationRequest>().SingleTenanted();
        });

        return services;
    }
    
    /// <summary>
    /// Register runtime configuration specific for the identity project.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseIdentity(this WebApplication app)
    {
        return app;
    }

    public static IServiceCollection AddOpenIddict(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var encryptionKey = configuration.GetSection("OpenIddict")["EncryptionKey"];
        var identityBaseUrl = configuration.GetSection("HttpRoutes")["IdentityBaseUrl"];

        // External signin provides
        var microsoftClientId =
            configuration.GetSection("Authentication:OpenIddict:ExternalProviders:Microsoft")["ClientId"];
        var microsoftClientSecret =
            configuration.GetSection("Authentication:OpenIddict:ExternalProviders:Microsoft")["ClientSecret"];
        var googleClientId =
            configuration.GetSection("Authentication:OpenIddict:ExternalProviders:Google")["ClientId"];
        var googleClientSecret =
            configuration.GetSection("Authentication:OpenIddict:ExternalProviders:Google")["ClientSecret"];


        Guard.IsNotNullOrEmpty(encryptionKey, "Encryption key");
        Guard.IsNotNullOrEmpty(identityBaseUrl, "CleanIAM.Identity Base Url");
        Guard.IsNotNullOrEmpty(microsoftClientId, "Microsoft Client Id");
        Guard.IsNotNullOrEmpty(microsoftClientSecret, "Microsoft Client Secret");
        Guard.IsNotNullOrEmpty(googleClientId, "Google Client Id");
        Guard.IsNotNullOrEmpty(googleClientSecret, "Google Client Secret");


        serviceCollection.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>()
                    .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                    .SetTokenEndpointUris("/connect/token")
                    .SetIntrospectionEndpointUris("/connect/introspect")
                    .SetEndSessionEndpointUris("/connect/endsession")
                    .SetUserInfoEndpointUris("/connect/userinfo");  

                options.AllowAuthorizationCodeFlow(); // For FE clients
                options.AllowClientCredentialsFlow() // For BE clients
                    .RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();

                options.AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableStatusCodePagesIntegration();

                options.DisableAccessTokenEncryption();

                options.AddEncryptionKey(new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(encryptionKey)));
            })
            .AddClient(options =>
            {
                // Allow the OpenIddict client to negotiate the authorization code flow.
                options.AllowAuthorizationCodeFlow();

                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                options.UseAspNetCore()
                    .EnableRedirectionEndpointPassthrough();

                options.UseWebProviders()
                    .AddMicrosoft(config =>
                    {
                        config.SetClientId(microsoftClientId);
                        config.SetClientSecret(microsoftClientSecret);
                        config.SetRedirectUri("external-providers/callback/microsoft");
                        config.AddScopes("email", "profile", "openid");
                    })
                    .AddGoogle(config =>
                    {
                        config.SetClientId(googleClientId);
                        config.SetClientSecret(googleClientSecret);
                        config.SetRedirectUri("external-providers/callback/google");
                        config.AddScopes("email", "profile", "openid");
                    });
            })
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();

                // Enable authorization entry validation, which is required to be able
                // to reject access tokens retrieved from a revoked authorization code.
                options.EnableAuthorizationEntryValidation();
            });
        
        return serviceCollection;
    }
}