using System.Net;

namespace AxisEndpoints;

/// <summary>
/// The return type of <see cref="IEndpoint{TRequest,TResult}.HandleAsync"/> for JSON responses.
/// Carries the response body, HTTP status code, and optional headers as a single value.
///
/// Only <see cref="Body"/> is required; <see cref="StatusCode"/> defaults to 200 OK and
/// <see cref="Headers"/> defaults to empty.
///
/// <code>
/// // Minimal — 200 OK with a JSON body
/// return new Response&lt;UserResponse&gt; { Body = user };
///
/// // With status code
/// return new Response&lt;UserResponse&gt; { StatusCode = HttpStatusCode.Created, Body = user };
///
/// // With headers
/// return new Response&lt;UserResponse&gt;
/// {
///     StatusCode = HttpStatusCode.OK,
///     Headers = [("X-Request-Id", requestId)],
///     Body = user,
/// };
/// </code>
///
/// For endpoints that return no body, use <c>Response&lt;EmptyResponse&gt;</c> and set
/// <see cref="Body"/> to <see cref="EmptyResponse.Instance"/>, or use the
/// <see cref="Response.Empty"/> / <see cref="Response.NoContent"/> shorthands.
/// </summary>
public sealed class Response<TBody>
{
    /// <summary>HTTP status code. Defaults to 200 OK.</summary>
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

    /// <summary>Optional response headers applied before the body is written.</summary>
    public IReadOnlyList<(string Name, string Value)> Headers { get; init; } = [];

    /// <summary>The response body to serialize as JSON, or <see cref="EmptyResponse.Instance"/> for no body.</summary>
    public required TBody Body { get; init; }
}

/// <summary>
/// Static shorthands for common no-body responses.
/// </summary>
public static class Response
{
    /// <summary>200 OK with no body.</summary>
    public static Response<EmptyResponse> Empty { get; } = new() { Body = EmptyResponse.Instance };

    /// <summary>204 No Content.</summary>
    public static Response<EmptyResponse> NoContent { get; } =
        new() { StatusCode = HttpStatusCode.NoContent, Body = EmptyResponse.Instance };
}
