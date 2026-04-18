namespace AxisEndpoints;

/// <summary>
/// Marker type for endpoints that return no response body.
/// Use as the TResponse type parameter: <c>IEndpoint&lt;TRequest, EmptyResponse&gt;</c>.
/// <see cref="IResponseSender{TResponse}"/> skips JSON serialization when this type is sent.
/// </summary>
public sealed class EmptyResponse
{
    public static readonly EmptyResponse Instance = new();

    private EmptyResponse() { }
}
