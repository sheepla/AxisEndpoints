using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AxisEndpoints.Internal;

/// <summary>
/// Collects status code, headers, and response body, then materializes them as an
/// <see cref="IResult"/> so the Minimal API pipeline can infer the response type for
/// OpenAPI schema generation.
/// </summary>
internal sealed class ResponseSender<TResponse> : IResponseSender<TResponse>
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private readonly List<(string Name, string Value)> _headers = [];

    // Set by SendAsync; the handler awaits this task and then returns the captured result.
    internal IResult? Result { get; private set; }

    IResponseSender<TResponse> IResponseSender<TResponse>.StatusCode(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    IResponseSender<TResponse> IResponseSender<TResponse>.Header(string name, string value)
    {
        _headers.Add((name, value));
        return this;
    }

    Task IResponseSender<TResponse>.SendAsync(TResponse response, CancellationToken cancel)
    {
        // Build an IResult rather than writing directly to the response stream.
        // Minimal API reads the generic type argument of Ok<T> / Json<T> to populate
        // the OpenAPI response schema — writing to HttpResponse bypasses this inference.
        IResult result =
            response is EmptyResponse
                ? Results.StatusCode((int)_statusCode)
                : Results.Json(response, statusCode: (int)_statusCode);

        // Wrap with headers if any were set.
        Result = _headers.Count > 0 ? new HeadersResult(result, _headers) : result;

        return Task.CompletedTask;
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
