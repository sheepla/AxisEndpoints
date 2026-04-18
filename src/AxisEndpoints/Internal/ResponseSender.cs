using System.Net;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Internal;

internal sealed class ResponseSender<TResponse> : IResponseSender<TResponse>
{
    private readonly HttpContext _context;
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private readonly List<(string Name, string Value)> _headers = [];

    internal ResponseSender(HttpContext context)
    {
        _context = context;
    }

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

    async Task IResponseSender<TResponse>.SendAsync(TResponse response, CancellationToken cancel)
    {
        _context.Response.StatusCode = (int)_statusCode;

        foreach (var (name, value) in _headers)
        {
            _context.Response.Headers[name] = value;
        }

        // EmptyResponse signals that no body should be written.
        if (response is EmptyResponse)
        {
            return;
        }

        await _context.Response.WriteAsJsonAsync(response, cancel);
    }
}
