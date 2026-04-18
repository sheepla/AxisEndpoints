using System.Reflection;
using AxisEndpoints.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AxisEndpoints.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Scans the entry assembly for endpoints and maps them to the Minimal API pipeline.
    /// </summary>
    public static WebApplication MapAxisEndpoints(this WebApplication app)
    {
        var entryAssembly =
            Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException(
                "Could not resolve the entry assembly. Use MapAxisEndpoints(Assembly) instead."
            );

        return app.MapAxisEndpoints(entryAssembly);
    }

    /// <summary>
    /// Scans the specified assembly for endpoints and maps them to the Minimal API pipeline.
    /// </summary>
    public static WebApplication MapAxisEndpoints(this WebApplication app, Assembly assembly)
    {
        return app.MapAxisEndpoints([assembly]);
    }

    /// <summary>
    /// Scans the specified assemblies for endpoints and maps them to the Minimal API pipeline.
    /// </summary>
    public static WebApplication MapAxisEndpoints(
        this WebApplication app,
        IEnumerable<Assembly> assemblies
    )
    {
        // Options are registered as a singleton by AddAxisEndpoints.
        // If missing, a default instance is used so MapAxisEndpoints works independently.
        var options = app.Services.GetService<AxisEndpointsOptions>() ?? new AxisEndpointsOptions();

        EndpointRegistry.MapEndpoints(app, assemblies, options);
        return app;
    }
}
