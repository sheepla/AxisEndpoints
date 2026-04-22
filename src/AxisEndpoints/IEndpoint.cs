namespace AxisEndpoints;

/// <summary>
/// Defines an endpoint that accepts a typed request body and returns a typed result.
/// Use this for POST, PUT, and PATCH endpoints.
/// For GET and DELETE, use <see cref="IEndpoint{TRequest,TResult}"/> with <c>[FromRoute]</c>
/// or <c>[FromQuery]</c> attributes on the request properties.
///
/// <typeparamref name="TResult"/> must be either:
/// <list type="bullet">
///   <item><see cref="Response{TBody}"/> — serialized as a JSON response.</item>
///   <item>An <see cref="Microsoft.AspNetCore.Http.IResult"/> implementation — executed directly,
///   allowing full control over the response (e.g. <c>CsvResponse&lt;T&gt;</c> from the CsvHelper extension).</item>
/// </list>
/// </summary>
public interface IEndpoint<TRequest, TResult>
{
    void Configure(IEndpointConfiguration config);

    Task<TResult> HandleAsync(TRequest request, CancellationToken cancel);
}

/// <summary>
/// Defines an endpoint with no request parameters and a typed result.
/// Use this only when the endpoint truly takes no parameters at all.
/// If query parameters are needed, define a request type with <c>[FromQuery]</c> properties
/// and use <see cref="IEndpoint{TRequest,TResult}"/> instead.
/// </summary>
public interface IEndpoint<TResult>
{
    void Configure(IEndpointConfiguration config);

    Task<TResult> HandleAsync(CancellationToken cancel);
}
