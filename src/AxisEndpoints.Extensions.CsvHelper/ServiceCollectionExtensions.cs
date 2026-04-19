using Microsoft.Extensions.DependencyInjection;

namespace AxisEndpoints.Extensions.CsvHelper;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AxisEndpoints.Extensions.CsvHelper services with the DI container.
    ///
    /// Call this alongside <c>AddAxisEndpoints()</c> in <c>Program.cs</c> when any endpoint
    /// uses <see cref="CsvBindingExceptionFilter"/>:
    /// <code>
    /// builder.Services.AddAxisEndpoints();
    /// builder.Services.AddAxisEndpointsCsvHelper();
    /// </code>
    ///
    /// This is necessary because <c>AddAxisEndpoints()</c> scans only the entry assembly for
    /// filters, so filters defined in extension packages must be registered explicitly.
    /// </summary>
    public static IServiceCollection AddAxisEndpointsCsvHelper(this IServiceCollection services)
    {
        services.AddScoped<CsvBindingExceptionFilter>();
        return services;
    }
}
