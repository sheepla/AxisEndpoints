using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AxisEndpoints.Example.Filters;

/// <summary>
/// Records a simple audit log entry for mutating operations.
/// Demonstrates per-endpoint AddFilter usage — applied only to CreateUserEndpoint.
/// In a real application this would write to a persistent audit store.
/// </summary>
public class AuditFilter : IEndpointFilter
{
    private readonly ILogger<AuditFilter> _logger;

    public AuditFilter(ILogger<AuditFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var result = await next(context);

        // Log after the handler so the status code is available.
        _logger.LogInformation(
            "[Audit] {Method} {Path} responded {StatusCode}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            context.HttpContext.Response.StatusCode
        );

        return result;
    }
}
