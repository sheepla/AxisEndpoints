using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AxisEndpoints.Example.Filters;

/// <summary>
/// Logs the HTTP method, path, and response status code for each request.
/// Demonstrates group-level AddFilter usage via UsersEndpointGroup.
/// </summary>
public class LoggingFilter : IEndpointFilter
{
    private readonly ILogger<LoggingFilter> _logger;

    public LoggingFilter(ILogger<LoggingFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        _logger.LogInformation(
            "Request: {Method} {Path}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path
        );

        var result = await next(context);

        _logger.LogInformation("Response: {StatusCode}", context.HttpContext.Response.StatusCode);

        return result;
    }
}
