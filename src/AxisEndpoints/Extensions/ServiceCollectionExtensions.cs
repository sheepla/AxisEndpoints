using System.Reflection;
using AxisEndpoints.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace AxisEndpoints.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the entry assembly for endpoints and registers them with the DI container.
    /// </summary>
    public static IServiceCollection AddAxisEndpoints(this IServiceCollection services)
    {
        return services.AddAxisEndpoints(_ => { });
    }

    /// <summary>
    /// Scans the entry assembly for endpoints and registers them with the DI container.
    /// </summary>
    public static IServiceCollection AddAxisEndpoints(
        this IServiceCollection services,
        Action<AxisEndpointsOptions> configure
    )
    {
        var entryAssembly =
            Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException(
                "Could not resolve the entry assembly. Use AddAxisEndpoints(Assembly) instead."
            );

        return services.AddAxisEndpoints(entryAssembly, configure);
    }

    /// <summary>
    /// Scans the specified assembly for endpoints and registers them with the DI container.
    /// </summary>
    public static IServiceCollection AddAxisEndpoints(
        this IServiceCollection services,
        Assembly assembly
    )
    {
        return services.AddAxisEndpoints([assembly], _ => { });
    }

    /// <summary>
    /// Scans the specified assembly for endpoints and registers them with the DI container.
    /// </summary>
    public static IServiceCollection AddAxisEndpoints(
        this IServiceCollection services,
        Assembly assembly,
        Action<AxisEndpointsOptions> configure
    )
    {
        return services.AddAxisEndpoints([assembly], configure);
    }

    /// <summary>
    /// Scans the specified assemblies for endpoints and registers them with the DI container.
    /// </summary>
    public static IServiceCollection AddAxisEndpoints(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies
    )
    {
        return services.AddAxisEndpoints(assemblies, _ => { });
    }

    /// <summary>
    /// Scans the specified assemblies for endpoints and registers them with the DI container.
    /// </summary>
    public static IServiceCollection AddAxisEndpoints(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        Action<AxisEndpointsOptions> configure
    )
    {
        var options = new AxisEndpointsOptions();
        configure(options);

        // EndpointContext depends on IHttpContextAccessor; register both here so callers need not.
        services.AddHttpContextAccessor();
        services.AddScoped<EndpointContext>();

        // Options are stored as a singleton so MapAxisEndpoints can retrieve them at route-mapping time.
        services.AddSingleton(options);

        EndpointRegistry.RegisterServices(services, assemblies);
        return services;
    }
}
