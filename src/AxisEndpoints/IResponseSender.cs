using System.Net;

namespace AxisEndpoints;

/// <summary>
/// Sends a typed HTTP response.
/// Chain <see cref="StatusCode"/> and <see cref="Header"/> before calling <see cref="SendAsync"/>.
/// For endpoints that return no body, use <see cref="EmptyResponse"/> as TResponse.
/// </summary>
public interface IResponseSender<TResponse>
{
    IResponseSender<TResponse> StatusCode(HttpStatusCode statusCode);
    IResponseSender<TResponse> Header(string name, string value);
    Task SendAsync(TResponse response, CancellationToken cancel = default);
}
