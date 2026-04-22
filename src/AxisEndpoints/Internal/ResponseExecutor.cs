using System.Net;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Internal;

/// <summary>
/// Converts a <see cref="Response{TBody}"/> value returned from
/// <c>HandleAsync</c> into an <see cref="IResult"/> for the Minimal API pipeline.
///
/// Keeping this logic here rather than on <see cref="Response{TBody}"/> itself avoids
/// a dependency on <c>Microsoft.AspNetCore.Http</c> in the public API surface.
/// </summary>
internal static class ResponseExecutor
{
    // Called via reflection from EndpointRegistry.ToIResult when TResult is Response<TBody>.
    internal static IResult ToResult<TBody>(Response<TBody> response)
    {
        IResult body =
            response.Body is EmptyResponse
                ? Results.StatusCode((int)response.StatusCode)
                : Results.Json(response.Body, statusCode: (int)response.StatusCode);

        return response.Headers.Count > 0 ? new HeadersResult(body, response.Headers) : body;
    }
}

/// <summary>
/// Applies response headers before delegating to the inner <see cref="IResult"/>.
/// </summary>
internal sealed class HeadersResult : IResult
{
    private readonly IResult _inner;
    private readonly IReadOnlyList<(string Name, string Value)> _headers;

    internal HeadersResult(IResult inner, IReadOnlyList<(string Name, string Value)> headers)
    {
        _inner = inner;
        _headers = headers;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        foreach (var (name, value) in _headers)
        {
            httpContext.Response.Headers[name] = value;
        }

        await _inner.ExecuteAsync(httpContext);
    }
}
