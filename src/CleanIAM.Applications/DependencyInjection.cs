using Marten;
using Microsoft.AspNetCore.Identity;

namespace CleanIAM.Applications;

public static class DependencyInjection
{
    /// <summary>
    /// Register configuration specific for the applications project.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplications(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure custom mapster config
        MapsterConfig.Configure();
        
        // This slice uses only the openIddict so no documents have to be registered to marten

        return services;
    }

    /// <summary>
    /// Register runtime configuration specific for the applications project.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseApplications(this WebApplication app)
    {
        return app;
    }
}