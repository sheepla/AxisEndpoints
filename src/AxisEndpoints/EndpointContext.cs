using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AxisEndpoints;

/// <summary>
/// Provides controlled access to the current <see cref="HttpContext"/> via <see cref="IHttpContextAccessor"/>.
/// Inject this into an endpoint's constructor only when access beyond typed request/response is needed
/// (e.g. reading request headers, accessing the authenticated user, or writing a raw response stream).
/// Typed response sending is handled by <see cref="IResponseSender{TResponse}"/>.
/// </summary>
public sealed class EndpointContext
{
    private readonly IHttpContextAccessor _accessor;

    public EndpointContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private HttpContext Http =>
        _accessor.HttpContext
        ?? throw new InvalidOperationException(
            "EndpointContext is being used outside of an HTTP request scope."
        );

    // Request side: read-only access
    public IHeaderDictionary RequestHeaders => Http.Request.Headers;
    public ClaimsPrincipal User => Http.User;

    public string? GetRouteValue(string key) => Http.GetRouteValue(key)?.ToString();

    public IQueryCollection Query => Http.Request.Query;

    // Escape hatch for raw response writes that IResponseSender cannot express (e.g. binary streams, CSV)
    public HttpResponse RawResponse => Http.Response;
}
