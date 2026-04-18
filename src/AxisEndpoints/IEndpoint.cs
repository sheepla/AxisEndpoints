namespace AxisEndpoints;

/// <summary>
/// Defines an endpoint that accepts a typed request body and returns a typed response.
/// Use this for POST, PUT, and PATCH endpoints.
/// For GET and DELETE, use <see cref="IEndpoint{TRequest,TResponse}"/> with <c>[FromRoute]</c>
/// or <c>[FromQuery]</c> attributes on the request properties.
/// </summary>
public interface IEndpoint<TRequest, TResponse>
{
    void Configure(IEndpointConfiguration config);

    Task HandleAsync(IResponseSender<TResponse> sender, TRequest request, CancellationToken cancel);
}

/// <summary>
/// Defines an endpoint with no request parameters and a typed response.
/// Use this only when the endpoint truly takes no parameters at all.
/// If query parameters are needed, define a request type with <c>[FromQuery]</c> properties
/// and use <see cref="IEndpoint{TRequest,TResponse}"/> instead.
/// </summary>
public interface IEndpoint<TResponse>
{
    void Configure(IEndpointConfiguration config);

    Task HandleAsync(IResponseSender<TResponse> sender, CancellationToken cancel);
}
